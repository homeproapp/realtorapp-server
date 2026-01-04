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
        var property1 = CreateTestPropertyInvitation("123 Main St", "Toronto", "ON", "M5V3A8", "CA", agent.UserId);
        var property2 = CreateTestPropertyInvitation("456 Oak Ave", "Vancouver", "BC", "V6B1A1", "CA", agent.UserId);
        property2.AddressLine2 = "Suite 100";
        DbContext.SaveChanges();

        TestDataManager.CreateClientInvitationsProperty(invitation.ClientInvitationId, property1.PropertyInvitationId);
        TestDataManager.CreateClientInvitationsProperty(invitation.ClientInvitationId, property2.PropertyInvitationId);

        // Act
        var result = await InvitationService.ValidateClientInvitationAsync(invitation.InvitationToken);

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
        var result = await InvitationService.ValidateClientInvitationAsync(nonExistentToken);

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
        var expiredInvitation = TestDataManager.CreateClientInvitation(
            agentUserId: agent.UserId,
            email: "test@example.com",
            firstName: "John",
            lastName: "Doe",
            phone: null,
            expiresAt: DateTime.UtcNow.AddDays(-1) // Expired yesterday
        );

        // Act
        var result = await InvitationService.ValidateClientInvitationAsync(expiredInvitation.InvitationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Invalid invitation token", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateInvitationAsync_WithAlreadyAcceptedInvitation_ReturnsInvalid()
    {
        // Arrange
        var agent = CreateTestAgent();
        var acceptedInvitation = TestDataManager.CreateClientInvitation(
            agentUserId: agent.UserId,
            email: "test@example.com",
            firstName: "John",
            lastName: "Doe",
            phone: null,
            expiresAt: DateTime.UtcNow.AddDays(7),
            acceptedAt: DateTime.UtcNow.AddDays(-1) // Already accepted
        );

        // Act
        var result = await InvitationService.ValidateClientInvitationAsync(acceptedInvitation.InvitationToken);

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
        var result = await InvitationService.ValidateClientInvitationAsync(invitation.InvitationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Invalid invitation token", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateInvitationAsync_WithMinimalClientData_ReturnsValid()
    {
        // Arrange
        var agent = CreateTestAgent();
        var property = CreateTestPropertyInvitation("123 Test St", "Toronto", "ON", "M5V3A8", "CA", agent.UserId);

        var invitation = TestDataManager.CreateClientInvitation(
            agentUserId: agent.UserId,
            email: "minimal@example.com",
            firstName: "Minimal",
            lastName: "User",
            phone: null,
            expiresAt: DateTime.UtcNow.AddDays(7)
        );
        TestDataManager.CreateClientInvitationsProperty(invitation.ClientInvitationId, property.PropertyInvitationId);

        // Act
        var result = await InvitationService.ValidateClientInvitationAsync(invitation.InvitationToken);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("minimal@example.com", result.ClientEmail);
        Assert.Equal("Minimal", result.ClientFirstName);
        Assert.Equal("User", result.ClientLastName);
        Assert.Null(result.ClientPhone);
        Assert.Single(result.Properties);
    }

    [Fact]
    public async Task ValidateInvitationAsync_WithExactExpiryTime_ReturnsInvalid()
    {
        // Arrange
        var agent = CreateTestAgent();
        var invitation = TestDataManager.CreateClientInvitation(
            agentUserId: agent.UserId,
            email: "test@example.com",
            firstName: "John",
            lastName: "Doe",
            phone: null,
            expiresAt: DateTime.UtcNow // Expires exactly now
        );

        // Small delay to ensure we're past the expiry time
        await Task.Delay(10);

        // Act
        var result = await InvitationService.ValidateClientInvitationAsync(invitation.InvitationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Invalid invitation token", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateInvitationAsync_WithSoftDeletedInvitation_ReturnsInvalid()
    {
        // Arrange
        var agent = CreateTestAgent();
        var invitation = TestDataManager.CreateClientInvitation(
            agentUserId: agent.UserId,
            email: "test@example.com",
            firstName: "John",
            lastName: "Doe",
            phone: null,
            expiresAt: DateTime.UtcNow.AddDays(7),
            deletedAt: DateTime.UtcNow.AddMinutes(-5) // Soft deleted
        );

        // Act
        var result = await InvitationService.ValidateClientInvitationAsync(invitation.InvitationToken);

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

        var property1 = CreateTestPropertyInvitation("123 Main St", "Toronto", "ON", "M5V3A8", "CA", agent.UserId);
        var property2 = CreateTestPropertyInvitation("456 Oak Ave", "Vancouver", "BC", "V6B1A1", "CA", agent.UserId);
        property2.AddressLine2 = "Unit 5B";
        var property3 = CreateTestPropertyInvitation("789 Pine Rd", "Calgary", "AB", "T2P1A1", "CA", agent.UserId);
        DbContext.SaveChanges();

        TestDataManager.CreateClientInvitationsProperty(invitation.ClientInvitationId, property1.PropertyInvitationId);
        TestDataManager.CreateClientInvitationsProperty(invitation.ClientInvitationId, property2.PropertyInvitationId);
        TestDataManager.CreateClientInvitationsProperty(invitation.ClientInvitationId, property3.PropertyInvitationId);

        // Act
        var result = await InvitationService.ValidateClientInvitationAsync(invitation.InvitationToken);

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
