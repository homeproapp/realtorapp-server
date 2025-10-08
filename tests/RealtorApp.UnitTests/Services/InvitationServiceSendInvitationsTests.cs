using Microsoft.EntityFrameworkCore;
using Moq;
using RealtorApp.Contracts.Commands.Invitations;
using RealtorApp.Domain.DTOs;

namespace RealtorApp.UnitTests.Services;

public class InvitationServiceSendInvitationsTests : TestBase
{
    [Fact]
    public async Task SendInvitationsAsync_WithValidData_CreatesInvitationsAndSendsEmails()
    {
        // Arrange
        var agent = CreateTestAgent();
        var client1Email = $"client1{Guid.NewGuid():N}@example.com";
        var client2Email = $"client2{Guid.NewGuid():N}@example.com";
        var command = new SendInvitationCommand
        {
            Clients = new List<ClientInvitationRequest>
            {
                new ClientInvitationRequest { Email = client1Email, FirstName = "John", LastName = "Doe", Phone = "+1234567890" },
                new ClientInvitationRequest { Email = client2Email, FirstName = "Jane", LastName = "Smith" }
            },
            Properties = new List<PropertyInvitationRequest>
            {
                new PropertyInvitationRequest { AddressLine1 = "123 Main St", City = "Toronto", Region = "ON", PostalCode = "M5V3A8", CountryCode = "CA" },
                new PropertyInvitationRequest { AddressLine1 = "456 Oak Ave", City = "Vancouver", Region = "BC", PostalCode = "V6B1A1", CountryCode = "CA" }
            }
        };

        // Act
        var result = await InvitationService.SendInvitationsAsync(command, agent.UserId);

        // Assert
        Assert.True(result.IsSuccess());
        Assert.Equal(2, result.InvitationsSent);
        Assert.Empty(result.Errors);

        // Verify database records
        var clientInvitations = await DbContext.ClientInvitations
            .Where(ci => ci.InvitedBy == agent.UserId)
            .ToListAsync();
        Assert.Equal(2, clientInvitations.Count);

        var propertyInvitations = await DbContext.PropertyInvitations
            .Where(pi => pi.InvitedBy == agent.UserId)
            .ToListAsync();
        Assert.Equal(2, propertyInvitations.Count);

        var clientInvitationProperties = await DbContext.ClientInvitationsProperties
            .Where(cip => clientInvitations.Select(ci => ci.ClientInvitationId).Contains(cip.ClientInvitationId))
            .ToListAsync();
        Assert.Equal(4, clientInvitationProperties.Count); // 2 clients × 2 properties

        // Verify client invitation details
        var client1Invitation = clientInvitations.First(c => c.ClientEmail == client1Email);
        Assert.Equal("John", client1Invitation.ClientFirstName);
        Assert.Equal("Doe", client1Invitation.ClientLastName);
        Assert.Equal("+1234567890", client1Invitation.ClientPhone);
        Assert.Equal(agent.UserId, client1Invitation.InvitedBy);
        Assert.True(client1Invitation.ExpiresAt > DateTime.UtcNow.AddDays(6));

        // Verify property invitation details
        var property1 = propertyInvitations.First(p => p.AddressLine1 == "123 Main St");
        Assert.Equal("Toronto", property1.City);
        Assert.Equal("ON", property1.Region);
        Assert.Equal("M5V3A8", property1.PostalCode);
        Assert.Equal("CA", property1.CountryCode);
        Assert.Equal(agent.UserId, property1.InvitedBy);

        // Verify email service was called
        MockEmailService.Verify(x => x.SendBulkInvitationEmailsAsync(It.Is<List<InvitationEmailDto>>(
            emails => emails.Count == 2 &&
                     emails.All(e => e.ClientEmail.EndsWith("@example.com")))), Times.Once);
    }

    [Fact]
    public async Task SendInvitationsAsync_WithNonExistentAgent_ReturnsError()
    {
        // Arrange
        MockUserService.Setup(x => x.GetAgentName(999))
            .ReturnsAsync((string?)null);

        var command = new SendInvitationCommand
        {
            Clients = new List<ClientInvitationRequest>
            {
                new ClientInvitationRequest { Email = $"client{Guid.NewGuid():N}@example.com", FirstName = "John", LastName = "Doe" }
            },
            Properties = new List<PropertyInvitationRequest>
            {
                new PropertyInvitationRequest { AddressLine1 = "123 Main St", City = "Toronto", Region = "ON", PostalCode = "M5V3A8", CountryCode = "CA" }
            }
        };

        // Act
        var result = await InvitationService.SendInvitationsAsync(command, 999);

        // Assert
        Assert.False(result.IsSuccess());
        Assert.Contains("agent not found", result.Errors);
        Assert.Equal(0, result.InvitationsSent);

        // Verify no database records created for this agent
        var clientInvitations = await DbContext.ClientInvitations
            .Where(ci => ci.InvitedBy == 999)
            .ToListAsync();
        Assert.Empty(clientInvitations);
    }

    [Fact]
    public async Task SendInvitationsAsync_WithExistingUser_DetectsExistingUserCorrectly()
    {
        // Arrange
        var agent = CreateTestAgent();
        var existingClient = CreateTestClient();

        var command = new SendInvitationCommand
        {
            Clients = new List<ClientInvitationRequest>
            {
                new ClientInvitationRequest { Email = existingClient.User.Email, FirstName = "John", LastName = "Doe" }
            },
            Properties = new List<PropertyInvitationRequest>
            {
                new PropertyInvitationRequest { AddressLine1 = "123 Main St", City = "Toronto", Region = "ON", PostalCode = "M5V3A8", CountryCode = "CA" }
            }
        };

        // Act
        var result = await InvitationService.SendInvitationsAsync(command, agent.UserId);

        // Assert
        Assert.True(result.IsSuccess());
        Assert.Equal(1, result.InvitationsSent);

        // Verify encryption was called with existing user flag
        MockCryptoService.Verify(x => x.Encrypt(It.Is<string>(
            data => data.Contains("isExistingUser=True"))), Times.Once);
    }

    [Fact]
    public async Task SendInvitationsAsync_WithEmailFailures_ReturnsPartialSuccess()
    {
        // Arrange
        var agent = CreateTestAgent();
        var command = new SendInvitationCommand
        {
            Clients = new List<ClientInvitationRequest>
            {
                new ClientInvitationRequest { Email = $"client1{Guid.NewGuid():N}@example.com", FirstName = "John", LastName = "Doe" },
                new ClientInvitationRequest { Email = "client2@example.com", FirstName = "Jane", LastName = "Smith" }
            },
            Properties = new List<PropertyInvitationRequest>
            {
                new PropertyInvitationRequest { AddressLine1 = "123 Main St", City = "Toronto", Region = "ON", PostalCode = "M5V3A8", CountryCode = "CA" }
            }
        };

        // Setup email service to return one failed email
        var failedEmail = new InvitationEmailDto {
            ClientEmail = $"failed{Guid.NewGuid():N}@example.com",
            AgentName = "Test Agent",
            EncryptedData = "encrypted_data",
            ClientFirstName = "John",
            IsExistingUser = false
        };
        MockEmailService.Setup(x => x.SendBulkInvitationEmailsAsync(It.IsAny<List<InvitationEmailDto>>()))
            .ReturnsAsync(new List<InvitationEmailDto> { failedEmail });

        // Act
        var result = await InvitationService.SendInvitationsAsync(command, agent.UserId);

        // Assert
        Assert.False(result.IsSuccess());
        Assert.Equal(1, result.InvitationsSent);
        Assert.Contains("Failed to send invite to John", result.Errors);

        // Verify database records still created
        var clientInvitations = await DbContext.ClientInvitations.ToListAsync();
        Assert.Equal(2, clientInvitations.Count);
    }

    [Fact]
    public async Task SendInvitationsAsync_WithMultiplePropertiesAndClients_CreatesAllCombinations()
    {
        // Arrange
        var client1Guid = Guid.NewGuid();
        var agent = CreateTestAgent();
        var command = new SendInvitationCommand
        {
            Clients = new List<ClientInvitationRequest>
            {
                new ClientInvitationRequest { Email = $"client1{client1Guid:N}@example.com", FirstName = "John", LastName = "Doe" },
                new ClientInvitationRequest { Email = $"client2{Guid.NewGuid():N}@example.com", FirstName = "Jane", LastName = "Smith" },
                new ClientInvitationRequest { Email = $"client3{Guid.NewGuid():N}@example.com", FirstName = "Bob", LastName = "Wilson" }
            },
            Properties = new List<PropertyInvitationRequest>
            {
                new PropertyInvitationRequest { AddressLine1 = "123 Main St", City = "Toronto", Region = "ON", PostalCode = "M5V3A8", CountryCode = "CA" },
                new PropertyInvitationRequest { AddressLine1 = "456 Oak Ave", City = "Vancouver", Region = "BC", PostalCode = "V6B1A1", CountryCode = "CA" },
                new PropertyInvitationRequest { AddressLine1 = "789 Pine Rd", City = "Calgary", Region = "AB", PostalCode = "T2P1A1", CountryCode = "CA" }
            }
        };

        // Act
        var result = await InvitationService.SendInvitationsAsync(command, agent.UserId);

        // Assert
        Assert.True(result.IsSuccess());
        Assert.Equal(3, result.InvitationsSent);

        // Verify client-property combinations
        var clientInvitationProperties = await DbContext.ClientInvitationsProperties
            .Include(cip => cip.ClientInvitation)
            .Include(cip => cip.PropertyInvitation)
            .ToListAsync();

        Assert.Equal(9, clientInvitationProperties.Count); // 3 clients × 3 properties

        // Verify each client is associated with all properties
        var johnInvitation = await DbContext.ClientInvitations
            .Include(ci => ci.ClientInvitationsProperties)
            .ThenInclude(cip => cip.PropertyInvitation)
            .FirstAsync(ci => ci.ClientEmail == $"client1{client1Guid:N}@example.com");

        Assert.Equal(3, johnInvitation.ClientInvitationsProperties.Count);
        Assert.Contains(johnInvitation.ClientInvitationsProperties,
            cip => cip.PropertyInvitation.AddressLine1 == "123 Main St");
        Assert.Contains(johnInvitation.ClientInvitationsProperties,
            cip => cip.PropertyInvitation.AddressLine1 == "456 Oak Ave");
        Assert.Contains(johnInvitation.ClientInvitationsProperties,
            cip => cip.PropertyInvitation.AddressLine1 == "789 Pine Rd");
    }

    [Fact]
    public async Task SendInvitationsAsync_WithOptionalFields_HandlesMissingData()
    {
        // Arrange
        var agent = CreateTestAgent();
        var expectedEmail = $"client{Guid.NewGuid():N}@example.com";
        var command = new SendInvitationCommand
        {
            Clients = new List<ClientInvitationRequest>
            {
                new ClientInvitationRequest { Email = expectedEmail} // Only email provided
            },
            Properties = new List<PropertyInvitationRequest>
            {
                new PropertyInvitationRequest {
                    AddressLine1 = "123 Main St",
                    City = "Toronto",
                    Region = "ON",
                    PostalCode = "M5V3A8",
                    CountryCode = "CA"
                    // AddressLine2 missing
                }
            }
        };

        // Act
        var result = await InvitationService.SendInvitationsAsync(command, agent.UserId);

        // Assert
        Assert.True(result.IsSuccess());
        Assert.Equal(1, result.InvitationsSent);

        var clientInvitation = await DbContext.ClientInvitations.FirstAsync();
        Assert.Equal(expectedEmail, clientInvitation.ClientEmail);
        Assert.Null(clientInvitation.ClientFirstName);
        Assert.Null(clientInvitation.ClientLastName);
        Assert.Null(clientInvitation.ClientPhone);

        var propertyInvitation = await DbContext.PropertyInvitations.FirstAsync();
        Assert.Equal("123 Main St", propertyInvitation.AddressLine1);
        Assert.Null(propertyInvitation.AddressLine2);
    }

    [Fact]
    public async Task SendInvitationsAsync_GeneratesUniqueTokensForEachClient()
    {
        // Arrange
        var agent = CreateTestAgent();
        var command = new SendInvitationCommand
        {
            Clients = new List<ClientInvitationRequest>
            {
                new ClientInvitationRequest { Email = $"client1{Guid.NewGuid():N}@example.com", FirstName = "John" },
                new ClientInvitationRequest { Email = $"client2{Guid.NewGuid():N}@example.com", FirstName = "Jane" },
                new ClientInvitationRequest { Email = $"client3{Guid.NewGuid():N}@example.com", FirstName = "Bob" }
            },
            Properties = new List<PropertyInvitationRequest>
            {
                new PropertyInvitationRequest { AddressLine1 = "123 Main St", City = "Toronto", Region = "ON", PostalCode = "M5V3A8", CountryCode = "CA" }
            }
        };

        // Act
        var result = await InvitationService.SendInvitationsAsync(command, agent.UserId);

        // Assert
        Assert.True(result.IsSuccess());

        var clientInvitations = await DbContext.ClientInvitations.ToListAsync();
        var tokens = clientInvitations.Select(ci => ci.InvitationToken).ToList();

        Assert.Equal(3, tokens.Count);
        Assert.Equal(tokens.Distinct().Count(), tokens.Count); // All tokens should be unique
        Assert.All(tokens, token => Assert.NotEqual(Guid.Empty, token));
    }

    [Fact]
    public async Task SendInvitationsAsync_WithException_ReturnsErrorResponse()
    {
        // Arrange
        var agent = CreateTestAgent();
        var command = new SendInvitationCommand
        {
            Clients = new List<ClientInvitationRequest>
            {
                new ClientInvitationRequest { Email = $"client{Guid.NewGuid():N}@example.com", FirstName = "John" }
            },
            Properties = new List<PropertyInvitationRequest>
            {
                new PropertyInvitationRequest { AddressLine1 = "123 Main St", City = "Toronto", Region = "ON", PostalCode = "M5V3A8", CountryCode = "CA" }
            }
        };

        // Setup email service to throw exception
        MockEmailService.Setup(x => x.SendBulkInvitationEmailsAsync(It.IsAny<List<InvitationEmailDto>>()))
            .ThrowsAsync(new Exception("Email service error"));

        // Act
        var result = await InvitationService.SendInvitationsAsync(command, agent.UserId);

        // Assert
        Assert.False(result.IsSuccess());
        Assert.Contains("An unexpected error occurred", result.Errors);
        Assert.Equal(0, result.InvitationsSent);
    }
}