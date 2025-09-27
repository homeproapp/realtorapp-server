using Microsoft.EntityFrameworkCore;
using Moq;
using RealtorApp.Contracts.Commands.Invitations;
using RealtorApp.Domain.DTOs;
using RealtorApp.Domain.Models;
using Task = System.Threading.Tasks.Task;

namespace RealtorApp.UnitTests.Services;

public class InvitationServiceAcceptInvitationTests : TestBase
{
    [Fact]
    public async Task AcceptInvitationAsync_WithValidNewClient_CreatesUserAndClientRecords()
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
            City = "Vancouver",
            Region = "BC",
            PostalCode = "V6B1A1",
            CountryCode = "CA",
            InvitedBy = agent.UserId
        };

        DbContext.PropertyInvitations.AddRange(property1, property2);
        DbContext.ClientInvitationsProperties.AddRange(
            new ClientInvitationsProperty { ClientInvitationId = invitation.ClientInvitationId, PropertyInvitation = property1 },
            new ClientInvitationsProperty { ClientInvitationId = invitation.ClientInvitationId, PropertyInvitation = property2 }
        );
        await DbContext.SaveChangesAsync();

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
        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
        Assert.Equal("test_access_token", result.AccessToken);
        Assert.Equal("test_refresh_token", result.RefreshToken);

        // Verify User record created
        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Uuid == Guid.Parse(firebaseUid));
        Assert.NotNull(user);
        Assert.Equal(invitation.ClientEmail, user.Email);
        Assert.Equal(invitation.ClientFirstName, user.FirstName);
        Assert.Equal(invitation.ClientLastName, user.LastName);

        // Verify Client record created
        var client = await DbContext.Clients.FirstOrDefaultAsync(c => c.UserId == user.UserId);
        Assert.NotNull(client);

        // Verify Properties created
        var properties = await DbContext.Properties.ToListAsync();
        Assert.Equal(2, properties.Count);
        Assert.Contains(properties, p => p.AddressLine1 == "123 Main St");
        Assert.Contains(properties, p => p.AddressLine1 == "456 Oak Ave");

        // Verify ClientsProperties relationships created
        var clientProperties = await DbContext.ClientsProperties
            .Include(cp => cp.Property)
            .Where(cp => cp.ClientId == client.UserId)
            .ToListAsync();
        Assert.Equal(2, clientProperties.Count);
        Assert.All(clientProperties, cp => Assert.Equal(agent.UserId, cp.AgentId));

        // Verify invitation marked as accepted
        var updatedInvitation = await DbContext.ClientInvitations.FindAsync(invitation.ClientInvitationId);
        Assert.NotNull(updatedInvitation!.AcceptedAt);
        Assert.True(updatedInvitation.AcceptedAt > DateTime.UtcNow.AddMinutes(-1));

        // Verify JWT and refresh token services called
        MockJwtService.Verify(x => x.GenerateAccessToken(Guid.Parse(firebaseUid), "Client"), Times.Once);
        MockRefreshTokenService.Verify(x => x.CreateRefreshTokenAsync(client.UserId), Times.Once);
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithExistingClient_DoesNotCreateUserButCreatesProperties()
    {
        // Arrange
        var agent = CreateTestAgent();
        var firebaseUid = Guid.NewGuid().ToString();
        var existingClient = CreateTestClient(2, firebaseUid);

        var invitation = new ClientInvitation
        {
            ClientEmail = existingClient.User.Email,
            ClientFirstName = "Updated First",
            ClientLastName = "Updated Last",
            InvitationToken = Guid.NewGuid(),
            InvitedBy = agent.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        var property = new PropertyInvitation
        {
            AddressLine1 = "789 Pine Rd",
            City = "Calgary",
            Region = "AB",
            PostalCode = "T2P1A1",
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

        var authUserDto = new AuthProviderUserDto { Uid = firebaseUid, Email = existingClient.User.Email };
        MockAuthProviderService.Setup(x => x.ValidateTokenAsync("valid_firebase_token"))
            .ReturnsAsync(authUserDto);

        var command = new AcceptInvitationCommand
        {
            InvitationToken = invitation.InvitationToken,
            FirebaseToken = "valid_firebase_token"
        };

        var initialUserCount = await DbContext.Users.CountAsync();
        var initialClientCount = await DbContext.Clients.CountAsync();

        // Act
        var result = await InvitationService.AcceptInvitationAsync(command);

        // Assert
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);

        // Verify no new User/Client records created
        var finalUserCount = await DbContext.Users.CountAsync();
        var finalClientCount = await DbContext.Clients.CountAsync();
        Assert.Equal(initialUserCount, finalUserCount);
        Assert.Equal(initialClientCount, finalClientCount);

        // Verify new property and relationship created
        var newProperty = await DbContext.Properties.FirstOrDefaultAsync(p => p.AddressLine1 == "789 Pine Rd");
        Assert.NotNull(newProperty);

        var clientProperty = await DbContext.ClientsProperties
            .FirstOrDefaultAsync(cp => cp.ClientId == existingClient.UserId && cp.PropertyId == newProperty.PropertyId);
        Assert.NotNull(clientProperty);
        Assert.Equal(agent.UserId, clientProperty.AgentId);
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithExistingClientAndExistingProperty_UpdatesAgentRelationship()
    {
        // Arrange
        var agent1 = CreateTestAgent(1);
        var agent2 = CreateTestAgent(3);
        var firebaseUid = Guid.NewGuid().ToString();
        var existingClient = CreateTestClient(2, firebaseUid);

        // Create existing property with agent1
        var existingProperty = new Property
        {
            AddressLine1 = "123 Existing St",
            City = "Toronto",
            Region = "ON",
            PostalCode = "M5V3A8",
            CountryCode = "CA"
        };
        DbContext.Properties.Add(existingProperty);

        var existingClientProperty = new ClientsProperty
        {
            ClientId = existingClient.UserId,
            Property = existingProperty,
            AgentId = agent1.UserId
        };
        DbContext.ClientsProperties.Add(existingClientProperty);
        await DbContext.SaveChangesAsync();

        // Create invitation from agent2 for the same property
        var invitation = new ClientInvitation
        {
            ClientEmail = existingClient.User.Email,
            ClientFirstName = "Test",
            ClientLastName = "Client",
            InvitationToken = Guid.NewGuid(),
            InvitedBy = agent2.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        var propertyInvitation = new PropertyInvitation
        {
            AddressLine1 = "123 Existing St", // Same address as existing property
            City = "Toronto",
            Region = "ON",
            PostalCode = "M5V3A8",
            CountryCode = "CA",
            InvitedBy = agent2.UserId
        };

        DbContext.ClientInvitations.Add(invitation);
        DbContext.PropertyInvitations.Add(propertyInvitation);
        DbContext.ClientInvitationsProperties.Add(new ClientInvitationsProperty
        {
            ClientInvitationId = invitation.ClientInvitationId,
            PropertyInvitation = propertyInvitation
        });
        await DbContext.SaveChangesAsync();

        var authUserDto = new AuthProviderUserDto { Uid = firebaseUid, Email = existingClient.User.Email };
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

        // Verify old relationship is soft deleted
        var oldRelationship = await DbContext.ClientsProperties
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(cp => cp.ClientId == existingClient.UserId && cp.AgentId == agent1.UserId && cp.DeletedAt != null);
        Assert.NotNull(oldRelationship);

        // Verify new relationship created with agent2
        var newRelationship = await DbContext.ClientsProperties
            .Where(cp => cp.ClientId == existingClient.UserId && cp.AgentId == agent2.UserId && cp.DeletedAt == null)
            .FirstOrDefaultAsync();
        Assert.NotNull(newRelationship);
        Assert.Equal(existingProperty.PropertyId, newRelationship.PropertyId);
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithInvalidToken_ReturnsError()
    {
        // Arrange
        var command = new AcceptInvitationCommand
        {
            InvitationToken = Guid.NewGuid(),
            FirebaseToken = "valid_firebase_token"
        };

        // Act
        var result = await InvitationService.AcceptInvitationAsync(command);

        // Assert
        Assert.False(result.IsSuccess());
        Assert.Equal("Invalid invite", result.ErrorMessage);
        Assert.Null(result.AccessToken);
        Assert.Null(result.RefreshToken);
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithExpiredInvitation_ReturnsError()
    {
        // Arrange
        var agent = CreateTestAgent();
        var expiredInvitation = new ClientInvitation
        {
            ClientEmail = "test@example.com",
            InvitationToken = Guid.NewGuid(),
            InvitedBy = agent.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };

        DbContext.ClientInvitations.Add(expiredInvitation);
        await DbContext.SaveChangesAsync();

        var command = new AcceptInvitationCommand
        {
            InvitationToken = expiredInvitation.InvitationToken,
            FirebaseToken = "valid_firebase_token"
        };

        // Act
        var result = await InvitationService.AcceptInvitationAsync(command);

        // Assert
        Assert.False(result.IsSuccess());
        Assert.Equal("Invalid invite", result.ErrorMessage);
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithAlreadyAcceptedInvitation_ReturnsError()
    {
        // Arrange
        var agent = CreateTestAgent();
        var acceptedInvitation = new ClientInvitation
        {
            ClientEmail = "test@example.com",
            InvitationToken = Guid.NewGuid(),
            InvitedBy = agent.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            AcceptedAt = DateTime.UtcNow.AddDays(-1), // Already accepted
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        DbContext.ClientInvitations.Add(acceptedInvitation);
        await DbContext.SaveChangesAsync();

        var command = new AcceptInvitationCommand
        {
            InvitationToken = acceptedInvitation.InvitationToken,
            FirebaseToken = "valid_firebase_token"
        };

        // Act
        var result = await InvitationService.AcceptInvitationAsync(command);

        // Assert
        Assert.False(result.IsSuccess());
        Assert.Equal("Invalid invite", result.ErrorMessage);
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithInvalidFirebaseToken_ReturnsError()
    {
        // Arrange
        var agent = CreateTestAgent();
        var invitation = CreateTestClientInvitation(agent.UserId);

        MockAuthProviderService.Setup(x => x.ValidateTokenAsync("invalid_firebase_token"))
            .ReturnsAsync((AuthProviderUserDto?)null);

        var command = new AcceptInvitationCommand
        {
            InvitationToken = invitation.InvitationToken,
            FirebaseToken = "invalid_firebase_token"
        };

        // Act
        var result = await InvitationService.AcceptInvitationAsync(command);

        // Assert
        Assert.False(result.IsSuccess());
        Assert.Equal("Invalid invite", result.ErrorMessage);
        Assert.Null(result.AccessToken);
        Assert.Null(result.RefreshToken);

        // Verify invitation was not marked as accepted
        var unchangedInvitation = await DbContext.ClientInvitations.FindAsync(invitation.ClientInvitationId);
        Assert.Null(unchangedInvitation!.AcceptedAt);
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithEmailMismatch_ReturnsError()
    {
        // Arrange
        var agent = CreateTestAgent();
        var invitation = CreateTestClientInvitation(agent.UserId);

        var authUserDto = new AuthProviderUserDto
        {
            Uid = Guid.NewGuid().ToString(),
            Email = "different@example.com" // Different from invitation email
        };

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
        Assert.False(result.IsSuccess());
        Assert.Equal("Invalid invite", result.ErrorMessage);
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithMinimalInvitationData_HandlesNullFields()
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
            AddressLine1 = "123 Main St",
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

        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Uuid == Guid.Parse(firebaseUid));
        Assert.NotNull(user);
        Assert.Equal("minimal@example.com", user.Email);
        Assert.Null(user.FirstName);
        Assert.Null(user.LastName);
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithNoProperties_ReturnsError()
    {
        // Arrange
        var agent = CreateTestAgent();
        var invitation = CreateTestClientInvitation(agent.UserId);
        // No properties associated with this invitation - should be invalid

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
        Assert.False(result.IsSuccess());
        Assert.Equal("Invalid invite", result.ErrorMessage);
        Assert.Null(result.AccessToken);
        Assert.Null(result.RefreshToken);

        // Verify no User/Client records created
        var users = await DbContext.Users.Where(u => u.Uuid == Guid.Parse(firebaseUid)).ToListAsync();
        Assert.Empty(users);

        // Verify invitation was not marked as accepted
        var unchangedInvitation = await DbContext.ClientInvitations.FindAsync(invitation.ClientInvitationId);
        Assert.Null(unchangedInvitation!.AcceptedAt);
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithSoftDeletedInvitation_ReturnsError()
    {
        // Arrange
        var agent = CreateTestAgent();
        var invitation = new ClientInvitation
        {
            ClientEmail = "test@example.com",
            InvitationToken = Guid.NewGuid(),
            InvitedBy = agent.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            DeletedAt = DateTime.UtcNow.AddMinutes(-5), // Soft deleted
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        DbContext.ClientInvitations.Add(invitation);
        await DbContext.SaveChangesAsync();

        var command = new AcceptInvitationCommand
        {
            InvitationToken = invitation.InvitationToken,
            FirebaseToken = "valid_firebase_token"
        };

        // Act
        var result = await InvitationService.AcceptInvitationAsync(command);

        // Assert
        Assert.False(result.IsSuccess());
        Assert.Equal("Invalid invite", result.ErrorMessage);
    }
}