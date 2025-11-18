using Microsoft.EntityFrameworkCore;
using Moq;
using RealtorApp.Contracts.Commands.Invitations;
using RealtorApp.Domain.DTOs;
using RealtorApp.Domain.Models;
using Task = System.Threading.Tasks.Task;

namespace RealtorApp.UnitTests.Services;

public class InvitationServiceEdgeCaseTests : TestBase
{
    [Fact]
    public async Task SendInvitationsAsync_WithDuplicateClientEmails_IsUnsuccesful()
    {
        // Arrange
        var agent = CreateTestAgent();
        var command = new SendInvitationCommand
        {
            Clients = new List<ClientInvitationRequest>
            {
                new ClientInvitationRequest { Email = "duplicate@example.com", FirstName = "John", LastName = "Doe" },
                new ClientInvitationRequest { Email = "duplicate@example.com", FirstName = "Jane", LastName = "Doe" } // Same email
            },
            Properties = new List<PropertyInvitationRequest>
            {
                new PropertyInvitationRequest { AddressLine1 = "123 Main St", City = "Toronto", Region = "ON", PostalCode = "M5V3A8", CountryCode = "CA" }
            }
        };

        // Act
        var result = await InvitationService.SendInvitationsAsync(command, agent.UserId);

        // Assert
        Assert.False(result.IsSuccess());
        Assert.Equal(0, result.InvitationsSent);
    }

    [Fact]
    public async Task SendInvitationsAsync_WithDuplicatePropertyAddresses_CreatesMultiplePropertyInvitations()
    {
        // Arrange
        var agent = CreateTestAgent();
        var command = new SendInvitationCommand
        {
            Clients = new List<ClientInvitationRequest>
            {
                new ClientInvitationRequest { Email = "client@example.com", FirstName = "John", LastName = "Doe" }
            },
            Properties = new List<PropertyInvitationRequest>
            {
                new PropertyInvitationRequest { AddressLine1 = "123 Main St", City = "Toronto", Region = "ON", PostalCode = "M5V3A8", CountryCode = "CA" },
                new PropertyInvitationRequest { AddressLine1 = "123 Main St", City = "Toronto", Region = "ON", PostalCode = "M5V3A8", CountryCode = "CA" } // Duplicate
            }
        };

        // Act
        var result = await InvitationService.SendInvitationsAsync(command, agent.UserId);

        // Assert
        Assert.True(result.IsSuccess());
        Assert.Equal(1, result.InvitationsSent);

        var propertyInvitations = await DbContext.PropertyInvitations.ToListAsync();
        Assert.Equal(2, propertyInvitations.Count); // Both duplicate properties created

        var clientInvitationProperties = await DbContext.ClientInvitationsProperties.ToListAsync();
        Assert.Equal(2, clientInvitationProperties.Count); // Client associated with both
    }

    [Fact]
    public async Task AcceptInvitationAsync_ConcurrentAcceptance_HandlesRaceCondition()
    {
        // Arrange
        var agent = CreateTestAgent();
        var invitation = CreateTestClientInvitation(agent.UserId);

        var property = CreateTestPropertyInvitation("123 Main St", "Toronto", "ON", "M5V3A8", "CA", agent.UserId);

        TestDataManager.CreateClientInvitationsProperty(invitation.ClientInvitationId, property.PropertyInvitationId);

        var firebaseUid = Guid.NewGuid().ToString();
        var authUserDto = new AuthProviderUserDto { Uid = firebaseUid, Email = invitation.ClientEmail };
        MockAuthProviderService.Setup(x => x.ValidateTokenAsync("valid_firebase_token"))
            .ReturnsAsync(authUserDto);

        var command = new AcceptInvitationCommand
        {
            InvitationToken = invitation.InvitationToken,
            FirebaseToken = "valid_firebase_token"
        };

        // Act - Simulate first acceptance
        var result1 = await InvitationService.AcceptInvitationAsync(command);

        // Act - Simulate second concurrent acceptance attempt
        var result2 = await InvitationService.AcceptInvitationAsync(command);

        // Assert
        Assert.True(result1.IsSuccess());
        Assert.False(result2.IsSuccess());
        Assert.Equal("Invalid invite", result2.ErrorMessage);

        // Verify only one User/Client record created
        var users = await DbContext.Users.Where(u => u.Uuid == firebaseUid).ToListAsync();
        Assert.Single(users);

        var clients = await DbContext.Clients.Where(c => c.User.Uuid == firebaseUid).ToListAsync();
        Assert.Single(clients);
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithVeryLongAddresses_HandlesLargeData()
    {
        // Arrange
        var agent = CreateTestAgent();
        var invitation = CreateTestClientInvitation(agent.UserId);

        var longAddress = new string('A', 500); // Very long address
        var property = CreateTestPropertyInvitation(longAddress, "Toronto", "ON", "M5V3A8", "CA", agent.UserId);
        property.AddressLine2 = new string('B', 200);
        DbContext.SaveChanges();

        TestDataManager.CreateClientInvitationsProperty(invitation.ClientInvitationId, property.PropertyInvitationId);

        var firebaseUid = Guid.NewGuid().ToString();
        var authUserDto = new AuthProviderUserDto { Uid = firebaseUid, Email = invitation.ClientEmail };
        MockAuthProviderService.Setup(x => x.ValidateTokenAsync("valid_firebase_token"))
            .ReturnsAsync(authUserDto);

        var command = new AcceptInvitationCommand
        {
            InvitationToken = invitation.InvitationToken,
            FirebaseToken = "valid_firebase_token"
        };

        // Act
        var result = await InvitationService.AcceptInvitationAsync(command);

        // Assert
        Assert.True(result.IsSuccess());

        var createdProperty = await DbContext.Properties.FirstOrDefaultAsync(p => p.AddressLine1 == longAddress);
        Assert.NotNull(createdProperty);
        Assert.Equal(longAddress, createdProperty.AddressLine1);
    }

    [Fact]
    public async Task SendInvitationsAsync_WithSpecialCharactersInData_HandlesEncodingCorrectly()
    {
        // Arrange
        var agent = CreateTestAgent();
        var command = new SendInvitationCommand
        {
            Clients = new List<ClientInvitationRequest>
            {
                new ClientInvitationRequest {
                    Email = "test@éxample.com",
                    FirstName = "José",
                    LastName = "García-González",
                    Phone = "+1-234-567-8900"
                }
            },
            Properties = new List<PropertyInvitationRequest>
            {
                new PropertyInvitationRequest {
                    AddressLine1 = "123 Rüe de la Paix",
                    AddressLine2 = "Ñoño's Apartment",
                    City = "Montréal",
                    Region = "QC",
                    PostalCode = "H1A 1A1",
                    CountryCode = "CA"
                }
            }
        };

        // Act
        var result = await InvitationService.SendInvitationsAsync(command, agent.UserId);

        // Assert
        Assert.True(result.IsSuccess());
        Assert.Equal(1, result.InvitationsSent);

        var invitation = await DbContext.ClientInvitations.FirstAsync();
        Assert.Equal("test@éxample.com", invitation.ClientEmail);
        Assert.Equal("José", invitation.ClientFirstName);
        Assert.Equal("García-González", invitation.ClientLastName);

        var property = await DbContext.PropertyInvitations.FirstAsync();
        Assert.Equal("123 Rüe de la Paix", property.AddressLine1);
        Assert.Equal("Ñoño's Apartment", property.AddressLine2);
        Assert.Equal("Montréal", property.City);
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithPropertyAddressMatchingCaseInsensitive_UpdatesCorrectProperty()
    {
        // Arrange
        var agent = CreateTestAgent();
        var firebaseUid = Guid.NewGuid().ToString();
        var existingClient = CreateTestClient(2, firebaseUid);

        // Create existing property with different casing
        var existingProperty = CreateTestProperty("123 MAIN STREET", "Toronto", "ON", "M5V3A8", "CA");
        var existingListing = TestDataManager.CreateListing(existingProperty.PropertyId);
        var existingConversation = TestDataManager.CreateConversation(existingListing.ListingId);
        var existingClientListing = TestDataManager.CreateClientListing(existingListing.ListingId, existingClient.UserId);
        var existingAgentListing = TestDataManager.CreateAgentListing(existingListing.ListingId, agent.UserId);

        // Create invitation with lowercase address
        var propertyInvitation = CreateTestPropertyInvitation("123 main street", "Toronto", "ON", "M5V3A8", "CA", agent.UserId);

        var invitation = TestDataManager.CreateClientInvitation(
            agentUserId: agent.UserId,
            email: existingClient.User.Email,
            firstName: "Test",
            lastName: "Client",
            phone: null,
            expiresAt: DateTime.UtcNow.AddDays(7)
        );
        TestDataManager.CreateClientInvitationsProperty(invitation.ClientInvitationId, propertyInvitation.PropertyInvitationId);

        var authUserDto = new AuthProviderUserDto { Uid = firebaseUid.ToString(), Email = existingClient.User.Email };
        MockAuthProviderService.Setup(x => x.ValidateTokenAsync("valid_firebase_token"))
            .ReturnsAsync(authUserDto);

        var command = new AcceptInvitationCommand
        {
            InvitationToken = invitation.InvitationToken,
            FirebaseToken = "valid_firebase_token"
        };

        // Act
        var result = await InvitationService.AcceptInvitationAsync(command);

        // Assert
        Assert.True(result.IsSuccess());

        // Verify old agent listing is soft deleted
        var oldAgentListing = await DbContext.AgentsListings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(al => al.ListingId == existingListing.ListingId && al.AgentId == agent.UserId && al.DeletedAt != null);
        Assert.NotNull(oldAgentListing);

        // Verify new agent listing created with current agent
        var newAgentListing = await DbContext.AgentsListings
            .Include(al => al.Listing)
            .Where(al =>  al.AgentId == agent.UserId && al.DeletedAt == null)
            .FirstOrDefaultAsync();
        Assert.NotNull(newAgentListing);
        Assert.Equal(existingProperty.PropertyId, newAgentListing.Listing.PropertyId);

        // Verify only one property exists (no duplicate created)
        var allProperties = await DbContext.Properties.ToListAsync();
        Assert.Single(allProperties);
    }

    [Fact]
    public async Task ValidateInvitationAsync_WithBoundaryExpiryTime_HandlesPrecisionCorrectly()
    {
        // Arrange
        var agent = CreateTestAgent();
        var exactExpiryTime = DateTime.UtcNow.AddMilliseconds(100);
        var property = CreateTestPropertyInvitation("123 Test St", "Toronto", "ON", "M5V3A8", "CA", agent.UserId);

        var invitation = TestDataManager.CreateClientInvitation(
            agentUserId: agent.UserId,
            email: "test@example.com",
            firstName: "Test",
            lastName: "User",
            phone: null,
            expiresAt: exactExpiryTime
        );
        TestDataManager.CreateClientInvitationsProperty(invitation.ClientInvitationId, property.PropertyInvitationId);

        // Act - Validate just before expiry
        var resultBeforeExpiry = await InvitationService.ValidateInvitationAsync(invitation.InvitationToken);

        // Wait for expiry
        await Task.Delay(200);

        // Act - Validate just after expiry
        var resultAfterExpiry = await InvitationService.ValidateInvitationAsync(invitation.InvitationToken);

        // Assert
        Assert.True(resultBeforeExpiry.IsValid);
        Assert.False(resultAfterExpiry.IsValid);
    }

    [Fact]
    public async Task SendInvitationsAsync_WithEmptyCollections_HandlesGracefully()
    {
        // Arrange
        var agent = CreateTestAgent();
        var command = new SendInvitationCommand
        {
            Clients = new List<ClientInvitationRequest>(),
            Properties = new List<PropertyInvitationRequest>()
        };

        // Act
        var result = await InvitationService.SendInvitationsAsync(command, agent.UserId);

        // Assert
        Assert.True(result.IsSuccess());
        Assert.Equal(0, result.InvitationsSent);

        var invitations = await DbContext.ClientInvitations.ToListAsync();
        var properties = await DbContext.PropertyInvitations.ToListAsync();
        Assert.Empty(invitations);
        Assert.Empty(properties);
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithMalformedFirebaseUid_HandlesInvalidGuidGracefully()
    {
        // Arrange
        var agent = CreateTestAgent();
        var invitation = CreateTestClientInvitation(agent.UserId);

        var property = CreateTestPropertyInvitation("123 Test St", "Toronto", "ON", "M5V3A8", "CA", agent.UserId);

        TestDataManager.CreateClientInvitationsProperty(invitation.ClientInvitationId, property.PropertyInvitationId);

        var authUserDto = new AuthProviderUserDto
        {
            Uid = "not-a-valid-guid", // Invalid GUID format
            Email = invitation.ClientEmail
        };

        MockAuthProviderService.Setup(x => x.ValidateTokenAsync("valid_firebase_token"))
            .ReturnsAsync(authUserDto);

        var command = new AcceptInvitationCommand
        {
            InvitationToken = invitation.InvitationToken,
            FirebaseToken = "valid_firebase_token"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await InvitationService.AcceptInvitationAsync(command));

        Assert.IsType<FormatException>(exception.InnerException);
    }

    [Fact]
    public async Task SendInvitationsAsync_WithLargeNumberOfClientsAndProperties_PerformsWell()
    {
        // Arrange
        var agent = CreateTestAgent();
        var clients = Enumerable.Range(1, 50)
            .Select(i => new ClientInvitationRequest
            {
                Email = $"client{i}@example.com",
                FirstName = $"Client{i}",
                LastName = "Test"
            }).ToList();

        var properties = Enumerable.Range(1, 20)
            .Select(i => new PropertyInvitationRequest
            {
                AddressLine1 = $"{i * 10} Test Street",
                City = "Toronto",
                Region = "ON",
                PostalCode = "M5V3A8",
                CountryCode = "CA"
            }).ToList();

        var command = new SendInvitationCommand
        {
            Clients = clients,
            Properties = properties
        };

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await InvitationService.SendInvitationsAsync(command, agent.UserId);
        stopwatch.Stop();

        // Assert
        Assert.True(result.IsSuccess());
        Assert.Equal(50, result.InvitationsSent);

        var clientInvitations = await DbContext.ClientInvitations.ToListAsync();
        var propertyInvitations = await DbContext.PropertyInvitations.ToListAsync();
        var clientInvitationProperties = await DbContext.ClientInvitationsProperties.ToListAsync();

        Assert.Equal(50, clientInvitations.Count);
        Assert.Equal(20, propertyInvitations.Count);
        Assert.Equal(1000, clientInvitationProperties.Count); // 50 clients × 20 properties

        // Performance assertion - should complete within reasonable time
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"Operation took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithNullOrEmptyStringProperties_HandlesGracefully()
    {
        // Arrange
        var agent = CreateTestAgent();
        var property = CreateTestPropertyInvitation("123 Test St", "Toronto", "ON", "M5V3A8", "CA", agent.UserId);

        var invitation = TestDataManager.CreateClientInvitation(
            agentUserId: agent.UserId,
            email: "test@example.com",
            firstName: "Test",
            lastName: "User",
            phone: "   ", // Whitespace
            expiresAt: DateTime.UtcNow.AddDays(7)
        );
        TestDataManager.CreateClientInvitationsProperty(invitation.ClientInvitationId, property.PropertyInvitationId);

        var firebaseUid = Guid.NewGuid().ToString();
        var authUserDto = new AuthProviderUserDto { Uid = firebaseUid, Email = invitation.ClientEmail };
        MockAuthProviderService.Setup(x => x.ValidateTokenAsync("valid_firebase_token"))
            .ReturnsAsync(authUserDto);

        var command = new AcceptInvitationCommand
        {
            InvitationToken = invitation.InvitationToken,
            FirebaseToken = "valid_firebase_token"
        };

        // Act
        var result = await InvitationService.AcceptInvitationAsync(command);

        // Assert
        Assert.True(result.IsSuccess());

        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Uuid == firebaseUid);
        Assert.NotNull(user);
        Assert.Equal("Test", user.FirstName);
        Assert.Equal("User", user.LastName);
    }
}