using Microsoft.EntityFrameworkCore;
using RealtorApp.Domain.Models;
using Task = System.Threading.Tasks.Task;

namespace RealtorApp.UnitTests.Services;

public class InvitationServiceValidateInvitationTests : TestBase
{
    [Fact]
    public async Task ValidateInvitationAsync_WithValidToken_ReturnsValidResponse()
    {
        // Arrange
        var agent = CreateTestAgent();
        var invitation = CreateTestClientInvitation(agent.UserId);

        // Add properties to the invitation
        var property1 = new PropertyInvitation
        {
            AddressLine1 = "123 Main St",
            City = "Toronto",
            Region = "ON",
            PostalCode = "M5V3A8",
            CountryCode = "CA",
            InvitedBy = agent.UserId,
            CreatedAt = DateTime.UtcNow
        };

        var property2 = new PropertyInvitation
        {
            AddressLine1 = "456 Oak Ave",
            AddressLine2 = "Suite 100",
            City = "Vancouver",
            Region = "BC",
            PostalCode = "V6B1A1",
            CountryCode = "CA",
            InvitedBy = agent.UserId,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.PropertyInvitations.AddRange(property1, property2);

        var clientInvitationProperty1 = new ClientInvitationsProperty
        {
            ClientInvitationId = invitation.ClientInvitationId,
            PropertyInvitation = property1,
            CreatedAt = DateTime.UtcNow
        };

        var clientInvitationProperty2 = new ClientInvitationsProperty
        {
            ClientInvitationId = invitation.ClientInvitationId,
            PropertyInvitation = property2,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.ClientInvitationsProperties.AddRange(clientInvitationProperty1, clientInvitationProperty2);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await InvitationService.ValidateInvitationAsync(invitation.InvitationToken);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(invitation.ClientEmail, result.ClientEmail);
        Assert.Equal(invitation.ClientFirstName, result.ClientFirstName);
        Assert.Equal(invitation.ClientLastName, result.ClientLastName);
        Assert.Equal(invitation.ClientPhone, result.ClientPhone);
        Assert.Equal(2, result.Properties.Count);

        var property1Result = result.Properties.First(p => p.AddressLine1 == "123 Main St");
        Assert.Equal("Toronto", property1Result.City);
        Assert.Equal("ON", property1Result.Region);
        Assert.Equal("M5V3A8", property1Result.PostalCode);
        Assert.Equal("CA", property1Result.CountryCode);
        Assert.Equal("", property1Result.AddressLine2);

        var property2Result = result.Properties.First(p => p.AddressLine1 == "456 Oak Ave");
        Assert.Equal("Suite 100", property2Result.AddressLine2);
        Assert.Equal("Vancouver", property2Result.City);
    }

    [Fact]
    public async Task ValidateInvitationAsync_WithNonExistentToken_ReturnsInvalid()
    {
        // Arrange
        var nonExistentToken = Guid.NewGuid();

        // Act
        var result = await InvitationService.ValidateInvitationAsync(nonExistentToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Invalid invitation token", result.ErrorMessage);
        Assert.Null(result.ClientEmail);
        Assert.Empty(result.Properties);
    }

    [Fact]
    public async Task ValidateInvitationAsync_WithExpiredToken_ReturnsInvalid()
    {
        // Arrange
        var agent = CreateTestAgent();
        var expiredInvitation = new ClientInvitation
        {
            ClientEmail = "test@example.com",
            ClientFirstName = "John",
            ClientLastName = "Doe",
            InvitationToken = Guid.NewGuid(),
            InvitedBy = agent.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired yesterday
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };

        DbContext.ClientInvitations.Add(expiredInvitation);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await InvitationService.ValidateInvitationAsync(expiredInvitation.InvitationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Invalid invitation token", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateInvitationAsync_WithAlreadyAcceptedInvitation_ReturnsInvalid()
    {
        // Arrange
        var agent = CreateTestAgent();
        var acceptedInvitation = new ClientInvitation
        {
            ClientEmail = "test@example.com",
            ClientFirstName = "John",
            ClientLastName = "Doe",
            InvitationToken = Guid.NewGuid(),
            InvitedBy = agent.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            AcceptedAt = DateTime.UtcNow.AddDays(-1), // Already accepted
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        DbContext.ClientInvitations.Add(acceptedInvitation);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await InvitationService.ValidateInvitationAsync(acceptedInvitation.InvitationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Invalid invitation token", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateInvitationAsync_WithNoProperties_ReturnsInvalid()
    {
        // Arrange
        var agent = CreateTestAgent();
        var invitation = CreateTestClientInvitation(agent.UserId);
        // Intentionally no properties added - this should be invalid

        // Act
        var result = await InvitationService.ValidateInvitationAsync(invitation.InvitationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Invalid invitation token", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateInvitationAsync_WithMinimalClientData_ReturnsValid()
    {
        // Arrange
        var agent = CreateTestAgent();
        var invitation = new ClientInvitation
        {
            ClientEmail = "minimal@example.com",
            // No first name, last name, or phone
            InvitationToken = Guid.NewGuid(),
            InvitedBy = agent.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        var property = new PropertyInvitation
        {
            AddressLine1 = "123 Test St",
            City = "Toronto",
            Region = "ON",
            PostalCode = "M5V3A8",
            CountryCode = "CA",
            InvitedBy = agent.UserId
        };

        DbContext.ClientInvitations.Add(invitation);
        DbContext.PropertyInvitations.Add(property);
        DbContext.ClientInvitationsProperties.Add(new ClientInvitationsProperty
        {
            ClientInvitationId = invitation.ClientInvitationId,
            PropertyInvitation = property
        });
        await DbContext.SaveChangesAsync();

        // Act
        var result = await InvitationService.ValidateInvitationAsync(invitation.InvitationToken);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("minimal@example.com", result.ClientEmail);
        Assert.Null(result.ClientFirstName);
        Assert.Null(result.ClientLastName);
        Assert.Null(result.ClientPhone);
        Assert.Single(result.Properties);
    }

    [Fact]
    public async Task ValidateInvitationAsync_WithExactExpiryTime_ReturnsInvalid()
    {
        // Arrange
        var agent = CreateTestAgent();
        var invitation = new ClientInvitation
        {
            ClientEmail = "test@example.com",
            ClientFirstName = "John",
            InvitationToken = Guid.NewGuid(),
            InvitedBy = agent.UserId,
            ExpiresAt = DateTime.UtcNow, // Expires exactly now
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        DbContext.ClientInvitations.Add(invitation);
        await DbContext.SaveChangesAsync();

        // Small delay to ensure we're past the expiry time
        await Task.Delay(10);

        // Act
        var result = await InvitationService.ValidateInvitationAsync(invitation.InvitationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Invalid invitation token", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateInvitationAsync_WithSoftDeletedInvitation_ReturnsInvalid()
    {
        // Arrange
        var agent = CreateTestAgent();
        var invitation = new ClientInvitation
        {
            ClientEmail = "test@example.com",
            ClientFirstName = "John",
            InvitationToken = Guid.NewGuid(),
            InvitedBy = agent.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            DeletedAt = DateTime.UtcNow.AddMinutes(-5), // Soft deleted
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        DbContext.ClientInvitations.Add(invitation);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await InvitationService.ValidateInvitationAsync(invitation.InvitationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Invalid invitation token", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateInvitationAsync_WithMultiplePropertiesHavingDifferentStructures_ReturnsAllProperties()
    {
        // Arrange
        var agent = CreateTestAgent();
        var invitation = CreateTestClientInvitation(agent.UserId);

        var property1 = new PropertyInvitation
        {
            AddressLine1 = "123 Main St",
            City = "Toronto",
            Region = "ON",
            PostalCode = "M5V3A8",
            CountryCode = "CA",
            InvitedBy = agent.UserId
        };

        var property2 = new PropertyInvitation
        {
            AddressLine1 = "456 Oak Ave",
            AddressLine2 = "Unit 5B",
            City = "Vancouver",
            Region = "BC",
            PostalCode = "V6B1A1",
            CountryCode = "CA",
            InvitedBy = agent.UserId
        };

        var property3 = new PropertyInvitation
        {
            AddressLine1 = "789 Pine Rd",
            City = "Calgary",
            Region = "AB",
            PostalCode = "T2P1A1",
            CountryCode = "CA",
            InvitedBy = agent.UserId
        };

        DbContext.PropertyInvitations.AddRange(property1, property2, property3);

        DbContext.ClientInvitationsProperties.AddRange(
            new ClientInvitationsProperty { ClientInvitationId = invitation.ClientInvitationId, PropertyInvitation = property1 },
            new ClientInvitationsProperty { ClientInvitationId = invitation.ClientInvitationId, PropertyInvitation = property2 },
            new ClientInvitationsProperty { ClientInvitationId = invitation.ClientInvitationId, PropertyInvitation = property3 }
        );

        await DbContext.SaveChangesAsync();

        // Act
        var result = await InvitationService.ValidateInvitationAsync(invitation.InvitationToken);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(3, result.Properties.Count);

        var torontoProperty = result.Properties.First(p => p.City == "Toronto");
        Assert.Equal("123 Main St", torontoProperty.AddressLine1);
        Assert.Equal("", torontoProperty.AddressLine2);

        var vancouverProperty = result.Properties.First(p => p.City == "Vancouver");
        Assert.Equal("456 Oak Ave", vancouverProperty.AddressLine1);
        Assert.Equal("Unit 5B", vancouverProperty.AddressLine2);

        var calgaryProperty = result.Properties.First(p => p.City == "Calgary");
        Assert.Equal("789 Pine Rd", calgaryProperty.AddressLine1);
        Assert.Equal("", calgaryProperty.AddressLine2);
    }
}