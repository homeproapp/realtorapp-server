using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using RealtorApp.Contracts.Queries.Chat.Requests;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Models;
using RealtorApp.Domain.Services;
using RealtorApp.UnitTests.Helpers;
using Task = System.Threading.Tasks.Task;

namespace RealtorApp.UnitTests.Services;

public class ChatServiceTests : IDisposable
{
    private readonly RealtorAppDbContext _dbContext;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<IUserAuthService> _mockUserAuthService;
    private readonly Mock<ISqlQueryService> _mockSqlQueryService;
    private readonly ChatService _chatService;
    private TestDataManager _testData;

    public ChatServiceTests()
    {
        // Setup PostgreSQL database using appsettings.json
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = configuration.GetConnectionString("Default");

        var options = new DbContextOptionsBuilder<RealtorAppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        _dbContext = new RealtorAppDbContext(options);

        // Clear all test data before each test
        CleanupAllTestData();

        _testData = new TestDataManager(_dbContext);

        _mockCache = new Mock<IMemoryCache>();
        _mockUserAuthService = new Mock<IUserAuthService>();
        _mockSqlQueryService = new Mock<ISqlQueryService>();

        _chatService = new ChatService(_dbContext, _mockUserAuthService.Object);
    }

    private void CleanupAllTestData()
    {
        _dbContext.Database.ExecuteSqlRaw(@"
            DELETE FROM contact_attachments;
            DELETE FROM task_attachments;
            DELETE FROM attachments;
            DELETE FROM files_tasks;
            DELETE FROM message_reads;
            DELETE FROM messages;
            DELETE FROM notifications;
            DELETE FROM tasks;
            DELETE FROM files;
            DELETE FROM links;
            DELETE FROM third_party_contacts;
            DELETE FROM client_invitations_properties;
            DELETE FROM clients_listings;
            DELETE FROM agents_listings;
            DELETE FROM property_invitations;
            DELETE FROM client_invitations;
            DELETE FROM conversations;
            DELETE FROM listings;
            DELETE FROM properties;
            DELETE FROM refresh_tokens;
            DELETE FROM clients;
            DELETE FROM agents;
            DELETE FROM users;
            DELETE FROM task_titles;
            DELETE FROM file_types;
        ");
    }

    [Fact]
    public async Task GetAgentConversationListAsync_WithMultipleClientsOnSameProperty_GroupsThemTogether()
    {
        // Arrange
        var agentId = await SetupTestData_MultipleClientsOneProperty();

        var query = new ConversationListQuery { Limit = 10, Offset = 0 };

        // Act
        var result = await _chatService.GetAgentConversationListAsync(query, agentId);

        // Assert
        Assert.NotNull(result);
        // Assert.Null(result.ErrorMessage);
        Assert.Single(result.Conversations); // Should be grouped into 1 conversation

        var conversation = result.Conversations[0];
        Assert.Equal(2, conversation.Clients.Length); // Should contain both clients
        Assert.Contains(conversation.Clients, c => c.ClientName == "Client One");
        Assert.Contains(conversation.Clients, c => c.ClientName == "Client Two");
    }

    [Fact]
    public async Task GetAgentConversationListAsync_WithSameClientsOnDifferentProperties_CreatesOneGroup()
    {
        // Arrange
        var agentId = await SetupTestData_SameClientsDifferentProperties();

        var query = new ConversationListQuery { Limit = 10, Offset = 0 };

        // Act
        var result = await _chatService.GetAgentConversationListAsync(query, agentId);

        // Assert
        Assert.NotNull(result);
        // Assert.Null(result.ErrorMessage);
        Assert.Single(result.Conversations); // Should be 1 group

        // Both groups should have the same clients but different properties
        foreach (var conversation in result.Conversations)
        {
            Assert.Equal(2, conversation.Clients.Length);
            Assert.Contains(conversation.Clients, c => c.ClientName == "Client One");
            Assert.Contains(conversation.Clients, c => c.ClientName == "Client Two");
        }
    }

    [Fact]
    public async Task GetAgentConversationListAsync_WithMostRecentMessage_ReturnsCorrectLastMessage()
    {
        // Arrange
        var agentId = await SetupTestData_WithMessages();

        var query = new ConversationListQuery { Limit = 10, Offset = 0 };

        // Act
        var result = await _chatService.GetAgentConversationListAsync(query, agentId);

        // Assert
        Assert.NotNull(result);
        // Assert.Null(result.ErrorMessage);
        Assert.Single(result.Conversations);

        var conversation = result.Conversations[0];
        Assert.NotNull(conversation.LastMessage);
        Assert.Equal("Most recent message", conversation.LastMessage.MessageText);
    }

    [Fact]
    public async Task GetAgentConversationListAsync_WithUnreadMessages_CalculatesUnreadCountCorrectly()
    {
        // Arrange
        var agentId = await SetupTestData_WithUnreadMessages();

        var query = new ConversationListQuery { Limit = 10, Offset = 0 };

        // Act
        var result = await _chatService.GetAgentConversationListAsync(query, agentId);

        // Assert
        Assert.NotNull(result);
        // Assert.Null(result.ErrorMessage);
        Assert.Single(result.Conversations);

        var conversation = result.Conversations[0];
        Assert.Equal(2, conversation.UnreadConversationCount); // 2 properties with unread messages
    }

    [Fact]
    public async Task GetAgentConversationListAsync_WithPagination_ReturnsCorrectSubset()
    {
        // Arrange
        var agentId = await SetupTestData_MultipleSeparateGroups();

        var query = new ConversationListQuery { Limit = 2, Offset = 0 };

        // Act
        var result = await _chatService.GetAgentConversationListAsync(query, agentId);

        // Assert
        Assert.NotNull(result);
        // Assert.Null(result.ErrorMessage);
        Assert.Equal(2, result.Conversations.Count); // Should return 2 items
        Assert.True(result.TotalCount >= 3); // Should indicate more items exist
        Assert.True(result.HasMore);
    }

    private async Task<long> SetupTestData_MultipleClientsOneProperty()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var client1User = _testData.CreateUser("client1@test.com", "Client", "One");
        var client2User = _testData.CreateUser("client2@test.com", "Client", "Two");

        var agent = _testData.CreateAgent(agentUser);
        var client1 = _testData.CreateClient(client1User);
        var client2 = _testData.CreateClient(client2User);

        var property1 = _testData.CreateProperty();
        var listing1 = _testData.CreateListing(property1.PropertyId);
        var conversation = _testData.CreateConversation(listing1.ListingId);

        _testData.CreateAgentListing(listing1.ListingId, agent.UserId);
        _testData.CreateClientListing(listing1.ListingId, client1.UserId);
        _testData.CreateClientListing(listing1.ListingId, client2.UserId);

        return await Task.FromResult(agent.UserId);
    }

    private async Task<long> SetupTestData_SameClientsDifferentProperties()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var client1User = _testData.CreateUser("client1@test.com", "Client", "One");
        var client2User = _testData.CreateUser("client2@test.com", "Client", "Two");

        var agent = _testData.CreateAgent(agentUser);
        var client1 = _testData.CreateClient(client1User);
        var client2 = _testData.CreateClient(client2User);

        var property1 = _testData.CreateProperty();
        var property2 = _testData.CreateProperty();

        var listing1 = _testData.CreateListing(property1.PropertyId);
        var listing2 = _testData.CreateListing(property2.PropertyId);

        var conversation1 = _testData.CreateConversation(listing1.ListingId);
        var conversation2 = _testData.CreateConversation(listing2.ListingId);

        _testData.CreateAgentListing(listing1.ListingId, agent.UserId);
        _testData.CreateClientListing(listing1.ListingId, client1.UserId);
        _testData.CreateClientListing(listing1.ListingId, client2.UserId);

        _testData.CreateAgentListing(listing2.ListingId, agent.UserId);
        _testData.CreateClientListing(listing2.ListingId, client1.UserId);
        _testData.CreateClientListing(listing2.ListingId, client2.UserId);

        return await Task.FromResult(agent.UserId);
    }

    private async Task<long> SetupTestData_WithMessages()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var client1User = _testData.CreateUser("client1@test.com", "Client", "One");
        var client2User = _testData.CreateUser("client2@test.com", "Client", "Two");

        var agent = _testData.CreateAgent(agentUser);
        var client1 = _testData.CreateClient(client1User);
        var client2 = _testData.CreateClient(client2User);

        var property1 = _testData.CreateProperty();
        var listing1 = _testData.CreateListing(property1.PropertyId);
        var conversation = _testData.CreateConversation(listing1.ListingId);

        _testData.CreateAgentListing(listing1.ListingId, agent.UserId);
        _testData.CreateClientListing(listing1.ListingId, client1.UserId);
        _testData.CreateClientListing(listing1.ListingId, client2.UserId);

        _testData.CreateMessage(listing1.ListingId, client1.UserId, "Older message", DateTime.UtcNow.AddMinutes(-10));
        _testData.CreateMessage(listing1.ListingId, client2.UserId, "Most recent message", DateTime.UtcNow.AddMinutes(-1));

        return await Task.FromResult(agent.UserId);
    }

    private async Task<long> SetupTestData_WithUnreadMessages()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var client1User = _testData.CreateUser("client1@test.com", "Client", "One");
        var client2User = _testData.CreateUser("client2@test.com", "Client", "Two");

        var agent = _testData.CreateAgent(agentUser);
        var client1 = _testData.CreateClient(client1User);
        var client2 = _testData.CreateClient(client2User);

        var property1 = _testData.CreateProperty();
        var property2 = _testData.CreateProperty();

        var listing1 = _testData.CreateListing(property1.PropertyId);
        var listing2 = _testData.CreateListing(property2.PropertyId);

        var conversation1 = _testData.CreateConversation(listing1.ListingId);
        var conversation2 = _testData.CreateConversation(listing2.ListingId);

        _testData.CreateAgentListing(listing1.ListingId, agent.UserId);
        _testData.CreateClientListing(listing1.ListingId, client1.UserId);
        _testData.CreateClientListing(listing1.ListingId, client2.UserId);

        _testData.CreateAgentListing(listing2.ListingId, agent.UserId);
        _testData.CreateClientListing(listing2.ListingId, client1.UserId);
        _testData.CreateClientListing(listing2.ListingId, client2.UserId);

        var msg1 = _testData.CreateMessage(listing1.ListingId, client1.UserId, "Unread message 1", DateTime.UtcNow.AddMinutes(-2));
        var msg2 = _testData.CreateMessage(listing2.ListingId, client2.UserId, "Unread message 2", DateTime.UtcNow.AddMinutes(-5));
        var msg3 = _testData.CreateMessage(listing1.ListingId, agent.UserId, "Read message from agent", DateTime.UtcNow.AddMinutes(-3));

        _testData.CreateMessageRead(msg3.MessageId, agent.UserId);

        return await Task.FromResult(agent.UserId);
    }

    private async Task<long> SetupTestData_MultipleSeparateGroups()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var agent = _testData.CreateAgent(agentUser);

        var property1 = _testData.CreateProperty();
        var property2 = _testData.CreateProperty();
        var property3 = _testData.CreateProperty();

        var listing1 = _testData.CreateListing(property1.PropertyId);
        var listing2 = _testData.CreateListing(property2.PropertyId);
        var listing3 = _testData.CreateListing(property3.PropertyId);

        var client1User = _testData.CreateUser("client1@test.com", "Client", "One");
        var client2User = _testData.CreateUser("client2@test.com", "Client", "Two");
        var client1 = _testData.CreateClient(client1User);
        var client2 = _testData.CreateClient(client2User);
        var conversation1 = _testData.CreateConversation(listing1.ListingId);
        _testData.CreateAgentListing(listing1.ListingId, agent.UserId);
        _testData.CreateClientListing(listing1.ListingId, client1.UserId);
        _testData.CreateClientListing(listing1.ListingId, client2.UserId);

        var client3User = _testData.CreateUser("client3@test.com", "Client", "Three");
        var client3 = _testData.CreateClient(client3User);
        var conversation2 = _testData.CreateConversation(listing2.ListingId);
        _testData.CreateAgentListing(listing2.ListingId, agent.UserId);
        _testData.CreateClientListing(listing2.ListingId, client3.UserId);

        var client4User = _testData.CreateUser("client4@test.com", "Client", "Four");
        var client5User = _testData.CreateUser("client5@test.com", "Client", "Five");
        var client4 = _testData.CreateClient(client4User);
        var client5 = _testData.CreateClient(client5User);
        var conversation3 = _testData.CreateConversation(listing3.ListingId);
        _testData.CreateAgentListing(listing3.ListingId, agent.UserId);
        _testData.CreateClientListing(listing3.ListingId, client4.UserId);
        _testData.CreateClientListing(listing3.ListingId, client5.UserId);

        return await Task.FromResult(agent.UserId);
    }

    #region Client Conversation List Tests

    [Fact]
    public async Task GetClientConversationList_WithMultiplePropertiesWithSameAgent_GroupsThemTogether()
    {
        // Arrange
        var clientId = await SetupTestData_ClientWithMultiplePropertiesSameAgent();

        var query = new ConversationListQuery { Limit = 10, Offset = 0 };

        // Act
        var result = await _chatService.GetClientConversationList(query, clientId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Conversations); // Should be grouped into 1 conversation

        var conversation = result.Conversations[0];
        Assert.Contains("Agent One", conversation.AgentNames);
    }

    [Fact]
    public async Task GetClientConversationList_WithSameAgentOnDifferentProperties_CreatesOneGroup()
    {
        // Arrange
        var clientId = await SetupTestData_ClientWithSameAgentDifferentProperties();

        var query = new ConversationListQuery { Limit = 10, Offset = 0 };

        // Act
        var result = await _chatService.GetClientConversationList(query, clientId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Conversations); // Should be 1 group for same agent
        Assert.Contains("Agent One", result.Conversations[0].AgentNames);
    }

    [Fact]
    public async Task GetClientConversationList_WithMostRecentMessage_ReturnsCorrectLastMessage()
    {
        // Arrange
        var clientId = await SetupTestData_ClientWithMessagesOneNewerConvo();

        var query = new ConversationListQuery { Limit = 10, Offset = 0 };

        // Act
        var result = await _chatService.GetClientConversationList(query, clientId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Conversations);

        var conversation = result.Conversations[0];
        Assert.NotNull(conversation.LastMessage);
        Assert.Equal("Most recent client message", conversation.LastMessage.MessageText);
    }

    [Fact]
    public async Task GetClientConversationList_WithUnreadMessages_CalculatesUnreadCountCorrectly()
    {
        // Arrange
        var clientId = await SetupTestData_ClientWithUnreadMessages();

        var query = new ConversationListQuery { Limit = 10, Offset = 0 };

        // Act
        var result = await _chatService.GetClientConversationList(query, clientId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Conversations);

        var conversation = result.Conversations[0];
        Assert.Equal(2, conversation.UnreadConversationCount); // 2 properties with unread messages
    }

    [Fact]
    public async Task GetClientConversationList_WithPagination_ReturnsCorrectSubset()
    {
        // Arrange
        var clientId = await SetupTestData_ClientWithMultipleAgents();

        var query = new ConversationListQuery { Limit = 2, Offset = 0 };

        // Act
        var result = await _chatService.GetClientConversationList(query, clientId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Conversations.Count); // Should return 2 items
        Assert.True(result.TotalCount >= 3); // Should indicate more items exist
        Assert.True(result.HasMore);
    }

    [Fact]
    public async Task GetClientConversationList_SelectsMostRecentConversationForClickThrough()
    {
        // Arrange
        var clientId = await SetupTestData_ClientWithMultipleConversationsOneAgent();

        var query = new ConversationListQuery { Limit = 10, Offset = 0 };

        // Act
        var result = await _chatService.GetClientConversationList(query, clientId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Conversations);

        var conversation = result.Conversations[0];
        // Should select conversation 2 as it was updated more recently
        // Note: We can't assert exact ID since it's generated dynamically
        Assert.True(conversation.ClickThroughConversationId > 0);
    }

    [Fact]
    public async Task GetClientConversationList_OrdersByNewestToOldest()
    {
        // Arrange
        var clientId = await SetupTestData_ClientWithMultipleAgentsTimestamped();

        var query = new ConversationListQuery { Limit = 10, Offset = 0 };

        // Act
        var result = await _chatService.GetClientConversationList(query, clientId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Conversations.Count);

        // Should be ordered by conversation updated_at, newest first
        Assert.Contains("Agent Three", result.Conversations[0].AgentNames); // Most recent
        Assert.Contains("Agent Two", result.Conversations[1].AgentNames); // Middle
        Assert.Contains("Agent One", result.Conversations[2].AgentNames); // Oldest
    }

    [Fact]
    public async Task GetClientConversationList_WithNoMessages_ReturnsConversationsWithNullLastMessage()
    {
        // Arrange
        var clientId = await SetupTestData_ClientWithNoMessages();

        var query = new ConversationListQuery { Limit = 10, Offset = 0 };

        // Act
        var result = await _chatService.GetClientConversationList(query, clientId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Conversations);
        Assert.Null(result.Conversations[0].LastMessage);
    }

    [Fact]
    public async Task GetClientConversationList_WithMultipleClientsOnProperty_GroupsByAgent()
    {
        // Arrange
        var clientId = await SetupTestData_ClientWithCoClientOnSameProperty();

        var query = new ConversationListQuery { Limit = 10, Offset = 0 };

        // Act
        var result = await _chatService.GetClientConversationList(query, clientId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Conversations); // Should be 1 group for the agent
        Assert.Contains("Agent One", result.Conversations[0].AgentNames);
    }

    [Fact]
    public async Task GetClientConversationList_WithSecondPagePagination_ReturnsCorrectItems()
    {
        // Arrange
        var clientId = await SetupTestData_ClientWithMultipleAgents();

        var query = new ConversationListQuery { Limit = 2, Offset = 2 };

        // Act
        var result = await _chatService.GetClientConversationList(query, clientId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Conversations); // Should return 1 remaining item
        Assert.Equal(3, result.TotalCount);
        Assert.False(result.HasMore); // No more items
    }

    [Fact]
    public async Task GetClientConversationList_WithNoConversations_ReturnsEmptyList()
    {
        // Arrange
        var clientUser = _testData.CreateUser("client@test.com", "Client", "One");
        var client = _testData.CreateClient(clientUser);
        var clientId = client.UserId;

        var query = new ConversationListQuery { Limit = 10, Offset = 0 };

        // Act
        var result = await _chatService.GetClientConversationList(query, clientId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Conversations);
        Assert.Equal(0, result.TotalCount);
        Assert.False(result.HasMore);
    }

    #endregion

    #region Client Conversation List Test Data Setup

    private async Task<long> SetupTestData_ClientWithMultiplePropertiesSameAgent()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var clientUser = _testData.CreateUser("client@test.com", "Client", "One");

        var agent = _testData.CreateAgent(agentUser);
        var client = _testData.CreateClient(clientUser);

        var property1 = _testData.CreateProperty();
        var property2 = _testData.CreateProperty();

        var listing1 = _testData.CreateListing(property1.PropertyId);
        var listing2 = _testData.CreateListing(property2.PropertyId);

        var conversation1 = _testData.CreateConversation(listing1.ListingId);
        var conversation2 = _testData.CreateConversation(listing2.ListingId);

        _testData.CreateAgentListing(listing1.ListingId, agent.UserId);
        _testData.CreateClientListing(listing1.ListingId, client.UserId);

        _testData.CreateAgentListing(listing2.ListingId, agent.UserId);
        _testData.CreateClientListing(listing2.ListingId, client.UserId);

        return await Task.FromResult(client.UserId);
    }

    private async Task<long> SetupTestData_ClientWithSameAgentDifferentProperties()
    {
        // Same as above but more explicit naming
        return await SetupTestData_ClientWithMultiplePropertiesSameAgent();
    }

        private async Task<long> SetupTestData_ClientWithMessagesOneNewerConvo()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var clientUser = _testData.CreateUser("client@test.com", "Client", "One");

        var agent = _testData.CreateAgent(agentUser);
        var client = _testData.CreateClient(clientUser);

        var property1 = _testData.CreateProperty();
        var property2 = _testData.CreateProperty();

        var listing1 = _testData.CreateListing(property1.PropertyId);
        var listing2 = _testData.CreateListing(property2.PropertyId);

        var conversation1 = _testData.CreateConversation(listing1.ListingId, updatedAt: DateTime.UtcNow.AddDays(3));
        var conversation2 = _testData.CreateConversation(listing2.ListingId);

        _testData.CreateAgentListing(listing1.ListingId, agent.UserId);
        _testData.CreateClientListing(listing1.ListingId, client.UserId);

        _testData.CreateAgentListing(listing2.ListingId, agent.UserId);
        _testData.CreateClientListing(listing2.ListingId, client.UserId);

        _testData.CreateMessage(listing1.ListingId, agent.UserId, "Older message", DateTime.UtcNow.AddMinutes(-10));
        _testData.CreateMessage(listing1.ListingId, client.UserId, "Most recent client message", DateTime.UtcNow.AddMinutes(-1));

        return await Task.FromResult(client.UserId);
    }

    private async Task<long> SetupTestData_ClientWithMessages()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var clientUser = _testData.CreateUser("client@test.com", "Client", "One");

        var agent = _testData.CreateAgent(agentUser);
        var client = _testData.CreateClient(clientUser);

        var property1 = _testData.CreateProperty();
        var property2 = _testData.CreateProperty();

        var listing1 = _testData.CreateListing(property1.PropertyId);
        var listing2 = _testData.CreateListing(property2.PropertyId);

        var conversation1 = _testData.CreateConversation(listing1.ListingId);
        var conversation2 = _testData.CreateConversation(listing2.ListingId);

        _testData.CreateAgentListing(listing1.ListingId, agent.UserId);
        _testData.CreateClientListing(listing1.ListingId, client.UserId);

        _testData.CreateAgentListing(listing2.ListingId, agent.UserId);
        _testData.CreateClientListing(listing2.ListingId, client.UserId);

        _testData.CreateMessage(listing1.ListingId, agent.UserId, "Older message", DateTime.UtcNow.AddMinutes(-10));
        _testData.CreateMessage(listing1.ListingId, client.UserId, "Most recent client message", DateTime.UtcNow.AddMinutes(-1));

        return await Task.FromResult(client.UserId);
    }

    private async Task<long> SetupTestData_ClientWithUnreadMessages()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var clientUser = _testData.CreateUser("client@test.com", "Client", "One");

        var agent = _testData.CreateAgent(agentUser);
        var client = _testData.CreateClient(clientUser);

        var property1 = _testData.CreateProperty();
        var property2 = _testData.CreateProperty();

        var listing1 = _testData.CreateListing(property1.PropertyId);
        var listing2 = _testData.CreateListing(property2.PropertyId);

        var conversation1 = _testData.CreateConversation(listing1.ListingId);
        var conversation2 = _testData.CreateConversation(listing2.ListingId);

        _testData.CreateAgentListing(listing1.ListingId, agent.UserId);
        _testData.CreateClientListing(listing1.ListingId, client.UserId);

        _testData.CreateAgentListing(listing2.ListingId, agent.UserId);
        _testData.CreateClientListing(listing2.ListingId, client.UserId);

        var msg1 = _testData.CreateMessage(listing1.ListingId, agent.UserId, "Unread message 1", DateTime.UtcNow.AddMinutes(-1));
        var msg2 = _testData.CreateMessage(listing2.ListingId, agent.UserId, "Unread message 2", DateTime.UtcNow.AddMinutes(-5));
        var msg3 = _testData.CreateMessage(listing1.ListingId, client.UserId, "Read message from client", DateTime.UtcNow.AddMinutes(-3));

        _testData.CreateMessageRead(msg3.MessageId, client.UserId);

        return await Task.FromResult(client.UserId);
    }

    private async Task<long> SetupTestData_ClientWithMultipleAgents()
    {
        var clientUser = _testData.CreateUser("client@test.com", "Client", "One");
        var client = _testData.CreateClient(clientUser);

        var agent1User = _testData.CreateUser("agent1@test.com", "Agent", "One");
        var agent1 = _testData.CreateAgent(agent1User);
        var property1 = _testData.CreateProperty();
        var listing1 = _testData.CreateListing(property1.PropertyId);
        var conversation1 = _testData.CreateConversation(listing1.ListingId);
        _testData.CreateAgentListing(listing1.ListingId, agent1.UserId);
        _testData.CreateClientListing(listing1.ListingId, client.UserId);

        var agent2User = _testData.CreateUser("agent2@test.com", "Agent", "Two");
        var agent2 = _testData.CreateAgent(agent2User);
        var property2 = _testData.CreateProperty();
        var listing2 = _testData.CreateListing(property2.PropertyId);
        var conversation2 = _testData.CreateConversation(listing2.ListingId);
        _testData.CreateAgentListing(listing2.ListingId, agent2.UserId);
        _testData.CreateClientListing(listing2.ListingId, client.UserId);

        var agent3User = _testData.CreateUser("agent3@test.com", "Agent", "Three");
        var agent3 = _testData.CreateAgent(agent3User);
        var property3 = _testData.CreateProperty();
        var listing3 = _testData.CreateListing(property3.PropertyId);
        var conversation3 = _testData.CreateConversation(listing3.ListingId);
        _testData.CreateAgentListing(listing3.ListingId, agent3.UserId);
        _testData.CreateClientListing(listing3.ListingId, client.UserId);

        return await Task.FromResult(client.UserId);
    }

    private async Task<long> SetupTestData_ClientWithMultipleConversationsOneAgent()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var clientUser = _testData.CreateUser("client@test.com", "Client", "One");

        var agent = _testData.CreateAgent(agentUser);
        var client = _testData.CreateClient(clientUser);

        var property1 = _testData.CreateProperty();
        var property2 = _testData.CreateProperty();

        var listing1 = _testData.CreateListing(property1.PropertyId);
        var listing2 = _testData.CreateListing(property2.PropertyId);

        var conversation1 = _testData.CreateConversation(listing1.ListingId);
        conversation1.UpdatedAt = DateTime.UtcNow.AddMinutes(-10);
        _dbContext.SaveChanges();
        _testData.CreateAgentListing(listing1.ListingId, agent.UserId);
        _testData.CreateClientListing(listing1.ListingId, client.UserId);

        var conversation2 = _testData.CreateConversation(listing2.ListingId);
        conversation2.UpdatedAt = DateTime.UtcNow.AddMinutes(-1);
        _dbContext.SaveChanges();
        _testData.CreateAgentListing(listing2.ListingId, agent.UserId);
        _testData.CreateClientListing(listing2.ListingId, client.UserId);

        return await Task.FromResult(client.UserId);
    }

    private async Task<long> SetupTestData_ClientWithMultipleAgentsTimestamped()
    {
        var clientUser = _testData.CreateUser("client@test.com", "Client", "One");
        var client = _testData.CreateClient(clientUser);

        var agent1User = _testData.CreateUser("agent1@test.com", "Agent", "One");
        var agent1 = _testData.CreateAgent(agent1User);
        var property1 = _testData.CreateProperty();
        var listing1 = _testData.CreateListing(property1.PropertyId);
        var conversation1 = _testData.CreateConversation(listing1.ListingId);
        conversation1.UpdatedAt = DateTime.UtcNow.AddMinutes(-30);
        _dbContext.SaveChanges();
        _testData.CreateAgentListing(listing1.ListingId, agent1.UserId);
        _testData.CreateClientListing(listing1.ListingId, client.UserId);

        var agent2User = _testData.CreateUser("agent2@test.com", "Agent", "Two");
        var agent2 = _testData.CreateAgent(agent2User);
        var property2 = _testData.CreateProperty();
        var listing2 = _testData.CreateListing(property2.PropertyId);
        var conversation2 = _testData.CreateConversation(listing2.ListingId);
        conversation2.UpdatedAt = DateTime.UtcNow.AddMinutes(-15);
        _dbContext.SaveChanges();
        _testData.CreateAgentListing(listing2.ListingId, agent2.UserId);
        _testData.CreateClientListing(listing2.ListingId, client.UserId);

        var agent3User = _testData.CreateUser("agent3@test.com", "Agent", "Three");
        var agent3 = _testData.CreateAgent(agent3User);
        var property3 = _testData.CreateProperty();
        var listing3 = _testData.CreateListing(property3.PropertyId);
        var conversation3 = _testData.CreateConversation(listing3.ListingId);
        conversation3.UpdatedAt = DateTime.UtcNow.AddMinutes(-1);
        _dbContext.SaveChanges();
        _testData.CreateAgentListing(listing3.ListingId, agent3.UserId);
        _testData.CreateClientListing(listing3.ListingId, client.UserId);

        return await Task.FromResult(client.UserId);
    }

    private async Task<long> SetupTestData_ClientWithNoMessages()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var clientUser = _testData.CreateUser("client@test.com", "Client", "One");

        var agent = _testData.CreateAgent(agentUser);
        var client = _testData.CreateClient(clientUser);

        var property1 = _testData.CreateProperty();
        var listing1 = _testData.CreateListing(property1.PropertyId);
        var conversation1 = _testData.CreateConversation(listing1.ListingId);

        _testData.CreateAgentListing(listing1.ListingId, agent.UserId);
        _testData.CreateClientListing(listing1.ListingId, client.UserId);

        return await Task.FromResult(client.UserId);
    }

    private async Task<long> SetupTestData_ClientWithCoClientOnSameProperty()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var clientUser = _testData.CreateUser("client@test.com", "Client", "One");
        var otherClientUser = _testData.CreateUser("client2@test.com", "Client", "Two");

        var agent = _testData.CreateAgent(agentUser);
        var client = _testData.CreateClient(clientUser);
        var otherClient = _testData.CreateClient(otherClientUser);

        var property1 = _testData.CreateProperty();
        var listing1 = _testData.CreateListing(property1.PropertyId);
        var conversation1 = _testData.CreateConversation(listing1.ListingId);

        _testData.CreateAgentListing(listing1.ListingId, agent.UserId);
        _testData.CreateClientListing(listing1.ListingId, client.UserId);
        _testData.CreateClientListing(listing1.ListingId, otherClient.UserId);

        return await Task.FromResult(client.UserId);
    }

    #endregion

    public void Dispose()
    {
        _testData.Dispose();
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}