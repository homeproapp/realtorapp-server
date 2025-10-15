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

        var property1 = CreateTestPropertyInvitation("123 Main St", "Toronto", "ON", "M5V3A8", "CA", agent.UserId);
        var property2 = CreateTestPropertyInvitation("456 Oak Ave", "Vancouver", "BC", "V6B1A1", "CA", agent.UserId);

        TestDataManager.CreateClientInvitationsProperty(invitation.ClientInvitationId, property1.PropertyInvitationId);
        TestDataManager.CreateClientInvitationsProperty(invitation.ClientInvitationId, property2.PropertyInvitationId);

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

        // Verify Listings and ClientsListings relationships created
        var listings = await DbContext.Listings
            .Include(l => l.Property)
            .Where(l => properties.Select(p => p.PropertyId).Contains(l.PropertyId))
            .ToListAsync();
        Assert.Equal(2, listings.Count);

        var clientListings = await DbContext.ClientsListings
            .Where(cl => listings.Select(l => l.ListingId).Contains(cl.ListingId) && cl.ClientId == client.UserId)
            .ToListAsync();
        Assert.Equal(2, clientListings.Count);

        var agentListings = await DbContext.AgentsListings
            .Where(al => listings.Select(l => l.ListingId).Contains(al.ListingId) && al.AgentId == agent.UserId)
            .ToListAsync();
        Assert.Equal(2, agentListings.Count);

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
        var firebaseUid = Guid.NewGuid();
        var existingClient = CreateTestClient(2, firebaseUid);

        var property = CreateTestPropertyInvitation("789 Pine Rd", "Calgary", "AB", "T2P1A1", "CA", agent.UserId);

        var invitation = TestDataManager.CreateClientInvitation(
            agentUserId: agent.UserId,
            email: existingClient.User.Email,
            firstName: "Updated First",
            lastName: "Updated Last",
            phone: null,
            expiresAt: DateTime.UtcNow.AddDays(7)
        );
        TestDataManager.CreateClientInvitationsProperty(invitation.ClientInvitationId, property.PropertyInvitationId);

        var authUserDto = new AuthProviderUserDto { Uid = firebaseUid.ToString(), Email = existingClient.User.Email };
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

        // Verify new property, listing, and relationships created
        var newProperty = await DbContext.Properties.FirstOrDefaultAsync(p => p.AddressLine1 == "789 Pine Rd");
        Assert.NotNull(newProperty);

        var listing = await DbContext.Listings.FirstOrDefaultAsync(l => l.PropertyId == newProperty.PropertyId);
        Assert.NotNull(listing);

        var clientListing = await DbContext.ClientsListings
            .FirstOrDefaultAsync(cl => cl.ClientId == existingClient.UserId && cl.ListingId == listing.ListingId);
        Assert.NotNull(clientListing);

        var agentListing = await DbContext.AgentsListings
            .FirstOrDefaultAsync(al => al.AgentId == agent.UserId && al.ListingId == listing.ListingId);
        Assert.NotNull(agentListing);
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithExistingClientAndExistingProperty_UpdatesAgentRelationship()
    {
        // Arrange
        var agent1 = CreateTestAgent(1);
        var agent2 = CreateTestAgent(3);
        var firebaseUid = Guid.NewGuid();
        var existingClient = CreateTestClient(2, firebaseUid);

        // Create existing property with agent1
        var existingProperty = CreateTestProperty("123 Existing St", "Toronto", "ON", "M5V3A8", "CA");
        var existingListing = TestDataManager.CreateListing(existingProperty.PropertyId);
        var existingConversation = TestDataManager.CreateConversation(existingListing.ListingId);
        var existingClientListing = TestDataManager.CreateClientListing(existingListing.ListingId, existingClient.UserId);
        var existingAgentListing = TestDataManager.CreateAgentListing(existingListing.ListingId, agent1.UserId);

        // Create invitation from agent2 for the same property
        var propertyInvitation = CreateTestPropertyInvitation("123 Existing St", "Toronto", "ON", "M5V3A8", "CA", agent2.UserId);

        var invitation = TestDataManager.CreateClientInvitation(
            agentUserId: agent2.UserId,
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
            .FirstOrDefaultAsync(al => al.ListingId == existingListing.ListingId && al.AgentId == agent1.UserId && al.DeletedAt != null);
        Assert.NotNull(oldAgentListing);

        // Verify new agent listing created with agent2
        var newAgentListing = await DbContext.AgentsListings
            .Include(al => al.Listing)
            .Where(al => al.AgentId == agent2.UserId && al.DeletedAt == null)
            .FirstOrDefaultAsync();
        Assert.NotNull(newAgentListing);
        Assert.Equal(existingProperty.PropertyId, newAgentListing.Listing.PropertyId);
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
        var expiredInvitation = TestDataManager.CreateClientInvitation(
            agentUserId: agent.UserId,
            email: "test@example.com",
            firstName: "Test",
            lastName: "User",
            phone: null,
            expiresAt: DateTime.UtcNow.AddDays(-1) // Expired
        );

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
        var acceptedInvitation = TestDataManager.CreateClientInvitation(
            agentUserId: agent.UserId,
            email: "test@example.com",
            firstName: "Test",
            lastName: "User",
            phone: null,
            expiresAt: DateTime.UtcNow.AddDays(7),
            acceptedAt: DateTime.UtcNow.AddDays(-1) // Already accepted
        );

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
        var property = CreateTestPropertyInvitation("123 Main St", "Toronto", "ON", "M5V3A8", "CA", agent.UserId);

        var invitation = TestDataManager.CreateClientInvitation(
            agentUserId: agent.UserId,
            email: "minimal@example.com",
            firstName: "Minimal",
            lastName: "User",
            phone: null,
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

        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Uuid == Guid.Parse(firebaseUid));
        Assert.NotNull(user);
        Assert.Equal("minimal@example.com", user.Email);
        Assert.Equal("Minimal", user.FirstName);
        Assert.Equal("User", user.LastName);
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
        var invitation = TestDataManager.CreateClientInvitation(
            agentUserId: agent.UserId,
            email: "test@example.com",
            firstName: "Test",
            lastName: "User",
            phone: null,
            expiresAt: DateTime.UtcNow.AddDays(7),
            deletedAt: DateTime.UtcNow.AddMinutes(-5) // Soft deleted
        );

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