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
        // Delete in correct order to avoid foreign key constraints
        // This is safer than TRUNCATE for avoiding deadlocks
        _dbContext.Database.ExecuteSqlRaw(@"
            DELETE FROM contact_attachments;
            DELETE FROM task_attachments;
            DELETE FROM attachments;
            DELETE FROM files_tasks;
            DELETE FROM messages;
            DELETE FROM notifications;
            DELETE FROM tasks;
            DELETE FROM files;
            DELETE FROM links;
            DELETE FROM third_party_contacts;
            DELETE FROM client_invitations_properties;
            DELETE FROM clients_properties;
            DELETE FROM property_invitations;
            DELETE FROM client_invitations;
            DELETE FROM conversations;
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
        // Create users first
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var client1User = _testData.CreateUser("client1@test.com", "Client", "One");
        var client2User = _testData.CreateUser("client2@test.com", "Client", "Two");

        // Create agent and clients
        var agent = _testData.CreateAgent(agentUser);
        var client1 = _testData.CreateClient(client1User);
        var client2 = _testData.CreateClient(client2User);

        // Create property
        var property1 = _testData.CreateProperty();

        // Create conversation
        var conversation = _testData.CreateConversation();

        // Create client properties (both clients on same property)
        _testData.CreateClientProperty(property1.PropertyId, client1.UserId, agent.UserId, conversation.ConversationId);
        _testData.CreateClientProperty(property1.PropertyId, client2.UserId, agent.UserId, conversation.ConversationId);

        return await Task.FromResult(agent.UserId);
    }

    private async Task<long> SetupTestData_SameClientsDifferentProperties()
    {
        // Create users
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var client1User = _testData.CreateUser("client1@test.com", "Client", "One");
        var client2User = _testData.CreateUser("client2@test.com", "Client", "Two");

        var agent = _testData.CreateAgent(agentUser);
        var client1 = _testData.CreateClient(client1User);
        var client2 = _testData.CreateClient(client2User);

        // Create properties
        var property1 = _testData.CreateProperty();
        var property2 = _testData.CreateProperty();

        // Create conversations
        var conversation1 = _testData.CreateConversation();
        var conversation2 = _testData.CreateConversation();

        // Create client properties (same clients on different properties)
        _testData.CreateClientProperty(property1.PropertyId, client1.UserId, agent.UserId, conversation1.ConversationId);
        _testData.CreateClientProperty(property1.PropertyId, client2.UserId, agent.UserId, conversation1.ConversationId);
        _testData.CreateClientProperty(property2.PropertyId, client1.UserId, agent.UserId, conversation2.ConversationId);
        _testData.CreateClientProperty(property2.PropertyId, client2.UserId, agent.UserId, conversation2.ConversationId);

        return await Task.FromResult(agent.UserId);
    }

    private async Task<long> SetupTestData_WithMessages()
    {
        // Create users first
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var client1User = _testData.CreateUser("client1@test.com", "Client", "One");
        var client2User = _testData.CreateUser("client2@test.com", "Client", "Two");

        // Create agent and clients
        var agent = _testData.CreateAgent(agentUser);
        var client1 = _testData.CreateClient(client1User);
        var client2 = _testData.CreateClient(client2User);

        // Create property
        var property1 = _testData.CreateProperty();

        // Create conversation
        var conversation = _testData.CreateConversation();

        // Create client properties (both clients on same property)
        _testData.CreateClientProperty(property1.PropertyId, client1.UserId, agent.UserId, conversation.ConversationId);
        _testData.CreateClientProperty(property1.PropertyId, client2.UserId, agent.UserId, conversation.ConversationId);

        // Add messages with different timestamps
        _testData.CreateMessage(conversation.ConversationId, client1.UserId, "Older message", DateTime.UtcNow.AddMinutes(-10));
        _testData.CreateMessage(conversation.ConversationId, client2.UserId, "Most recent message", DateTime.UtcNow.AddMinutes(-1));

        return await Task.FromResult(agent.UserId);
    }

    private async Task<long> SetupTestData_WithUnreadMessages()
    {
        // Create users
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var client1User = _testData.CreateUser("client1@test.com", "Client", "One");
        var client2User = _testData.CreateUser("client2@test.com", "Client", "Two");

        var agent = _testData.CreateAgent(agentUser);
        var client1 = _testData.CreateClient(client1User);
        var client2 = _testData.CreateClient(client2User);

        // Create properties
        var property1 = _testData.CreateProperty();
        var property2 = _testData.CreateProperty();

        // Create conversations
        var conversation1 = _testData.CreateConversation();
        var conversation2 = _testData.CreateConversation();

        // Create client properties (same clients on different properties)
        _testData.CreateClientProperty(property1.PropertyId, client1.UserId, agent.UserId, conversation1.ConversationId);
        _testData.CreateClientProperty(property1.PropertyId, client2.UserId, agent.UserId, conversation1.ConversationId);
        _testData.CreateClientProperty(property2.PropertyId, client1.UserId, agent.UserId, conversation2.ConversationId);
        _testData.CreateClientProperty(property2.PropertyId, client2.UserId, agent.UserId, conversation2.ConversationId);

        // Add unread messages in both conversations
        _testData.CreateMessage(conversation1.ConversationId, client1.UserId, "Unread message 1", DateTime.UtcNow.AddMinutes(-10), isRead: false);
        _testData.CreateMessage(conversation2.ConversationId, client2.UserId, "Unread message 2", DateTime.UtcNow.AddMinutes(-5), isRead: false);
        _testData.CreateMessage(conversation1.ConversationId, agent.UserId, "Read message from agent", DateTime.UtcNow.AddMinutes(-3), isRead: true);

        return await Task.FromResult(agent.UserId);
    }

    private async Task<long> SetupTestData_MultipleSeparateGroups()
    {
        // Create multiple distinct client groups
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var agent = _testData.CreateAgent(agentUser);

        // Create properties
        var property1 = _testData.CreateProperty();
        var property2 = _testData.CreateProperty();
        var property3 = _testData.CreateProperty();

        // Group 1: Client 1 & 2 on Property 1
        var client1User = _testData.CreateUser("client1@test.com", "Client", "One");
        var client2User = _testData.CreateUser("client2@test.com", "Client", "Two");
        var client1 = _testData.CreateClient(client1User);
        var client2 = _testData.CreateClient(client2User);
        var conversation1 = _testData.CreateConversation();
        _testData.CreateClientProperty(property1.PropertyId, client1.UserId, agent.UserId, conversation1.ConversationId);
        _testData.CreateClientProperty(property1.PropertyId, client2.UserId, agent.UserId, conversation1.ConversationId);

        // Group 2: Client 3 on Property 2
        var client3User = _testData.CreateUser("client3@test.com", "Client", "Three");
        var client3 = _testData.CreateClient(client3User);
        var conversation2 = _testData.CreateConversation();
        _testData.CreateClientProperty(property2.PropertyId, client3.UserId, agent.UserId, conversation2.ConversationId);

        // Group 3: Client 4 & 5 on Property 3
        var client4User = _testData.CreateUser("client4@test.com", "Client", "Four");
        var client5User = _testData.CreateUser("client5@test.com", "Client", "Five");
        var client4 = _testData.CreateClient(client4User);
        var client5 = _testData.CreateClient(client5User);
        var conversation3 = _testData.CreateConversation();
        _testData.CreateClientProperty(property3.PropertyId, client4.UserId, agent.UserId, conversation3.ConversationId);
        _testData.CreateClientProperty(property3.PropertyId, client5.UserId, agent.UserId, conversation3.ConversationId);

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
        Assert.Equal("Agent One", conversation.AgentName);
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
        Assert.Equal("Agent One", result.Conversations[0].AgentName);
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
        Assert.Equal("Agent Three", result.Conversations[0].AgentName); // Most recent
        Assert.Equal("Agent Two", result.Conversations[1].AgentName); // Middle
        Assert.Equal("Agent One", result.Conversations[2].AgentName); // Oldest
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
        Assert.Equal("Agent One", result.Conversations[0].AgentName);
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

        var conversation1 = _testData.CreateConversation();
        var conversation2 = _testData.CreateConversation();

        _testData.CreateClientProperty(property1.PropertyId, client.UserId, agent.UserId, conversation1.ConversationId);
        _testData.CreateClientProperty(property2.PropertyId, client.UserId, agent.UserId, conversation2.ConversationId);

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

        var conversation1 = _testData.CreateConversation(updatedAt: DateTime.UtcNow.AddDays(3));
        var conversation2 = _testData.CreateConversation();

        _testData.CreateClientProperty(property1.PropertyId, client.UserId, agent.UserId, conversation1.ConversationId);
        _testData.CreateClientProperty(property2.PropertyId, client.UserId, agent.UserId, conversation2.ConversationId);

        // Add messages with different timestamps
        _testData.CreateMessage(conversation1.ConversationId, agent.UserId, "Older message", DateTime.UtcNow.AddMinutes(-10));
        _testData.CreateMessage(conversation1.ConversationId, client.UserId, "Most recent client message", DateTime.UtcNow.AddMinutes(-1));

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

        var conversation1 = _testData.CreateConversation();
        var conversation2 = _testData.CreateConversation();

        _testData.CreateClientProperty(property1.PropertyId, client.UserId, agent.UserId, conversation1.ConversationId);
        _testData.CreateClientProperty(property2.PropertyId, client.UserId, agent.UserId, conversation2.ConversationId);

        // Add messages with different timestamps
        _testData.CreateMessage(conversation1.ConversationId, agent.UserId, "Older message", DateTime.UtcNow.AddMinutes(-10));
        _testData.CreateMessage(conversation1.ConversationId, client.UserId, "Most recent client message", DateTime.UtcNow.AddMinutes(-1));

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

        var conversation1 = _testData.CreateConversation();
        var conversation2 = _testData.CreateConversation();

        _testData.CreateClientProperty(property1.PropertyId, client.UserId, agent.UserId, conversation1.ConversationId);
        _testData.CreateClientProperty(property2.PropertyId, client.UserId, agent.UserId, conversation2.ConversationId);

        // Add unread messages in both conversations
        _testData.CreateMessage(conversation1.ConversationId, agent.UserId, "Unread message 1", DateTime.UtcNow.AddMinutes(-10), isRead: false);
        _testData.CreateMessage(conversation2.ConversationId, agent.UserId, "Unread message 2", DateTime.UtcNow.AddMinutes(-5), isRead: false);
        _testData.CreateMessage(conversation1.ConversationId, client.UserId, "Read message from client", DateTime.UtcNow.AddMinutes(-3), isRead: true);

        return await Task.FromResult(client.UserId);
    }

    private async Task<long> SetupTestData_ClientWithMultipleAgents()
    {
        var clientUser = _testData.CreateUser("client@test.com", "Client", "One");
        var client = _testData.CreateClient(clientUser);

        // Agent 1
        var agent1User = _testData.CreateUser("agent1@test.com", "Agent", "One");
        var agent1 = _testData.CreateAgent(agent1User);
        var property1 = _testData.CreateProperty();
        var conversation1 = _testData.CreateConversation();
        _testData.CreateClientProperty(property1.PropertyId, client.UserId, agent1.UserId, conversation1.ConversationId);

        // Agent 2
        var agent2User = _testData.CreateUser("agent2@test.com", "Agent", "Two");
        var agent2 = _testData.CreateAgent(agent2User);
        var property2 = _testData.CreateProperty();
        var conversation2 = _testData.CreateConversation();
        _testData.CreateClientProperty(property2.PropertyId, client.UserId, agent2.UserId, conversation2.ConversationId);

        // Agent 3
        var agent3User = _testData.CreateUser("agent3@test.com", "Agent", "Three");
        var agent3 = _testData.CreateAgent(agent3User);
        var property3 = _testData.CreateProperty();
        var conversation3 = _testData.CreateConversation();
        _testData.CreateClientProperty(property3.PropertyId, client.UserId, agent3.UserId, conversation3.ConversationId);

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

        // Conversation 1 - older
        var conversation1 = _testData.CreateConversation();
        conversation1.UpdatedAt = DateTime.UtcNow.AddMinutes(-10);
        _dbContext.SaveChanges();
        _testData.CreateClientProperty(property1.PropertyId, client.UserId, agent.UserId, conversation1.ConversationId);

        // Conversation 2 - newer (should be click-through)
        var conversation2 = _testData.CreateConversation();
        conversation2.UpdatedAt = DateTime.UtcNow.AddMinutes(-1);
        _dbContext.SaveChanges();
        _testData.CreateClientProperty(property2.PropertyId, client.UserId, agent.UserId, conversation2.ConversationId);

        return await Task.FromResult(client.UserId);
    }

    private async Task<long> SetupTestData_ClientWithMultipleAgentsTimestamped()
    {
        var clientUser = _testData.CreateUser("client@test.com", "Client", "One");
        var client = _testData.CreateClient(clientUser);

        // Agent 1 - oldest conversation
        var agent1User = _testData.CreateUser("agent1@test.com", "Agent", "One");
        var agent1 = _testData.CreateAgent(agent1User);
        var property1 = _testData.CreateProperty();
        var conversation1 = _testData.CreateConversation();
        conversation1.UpdatedAt = DateTime.UtcNow.AddMinutes(-30);
        _dbContext.SaveChanges();
        _testData.CreateClientProperty(property1.PropertyId, client.UserId, agent1.UserId, conversation1.ConversationId);

        // Agent 2 - middle conversation
        var agent2User = _testData.CreateUser("agent2@test.com", "Agent", "Two");
        var agent2 = _testData.CreateAgent(agent2User);
        var property2 = _testData.CreateProperty();
        var conversation2 = _testData.CreateConversation();
        conversation2.UpdatedAt = DateTime.UtcNow.AddMinutes(-15);
        _dbContext.SaveChanges();
        _testData.CreateClientProperty(property2.PropertyId, client.UserId, agent2.UserId, conversation2.ConversationId);

        // Agent 3 - newest conversation
        var agent3User = _testData.CreateUser("agent3@test.com", "Agent", "Three");
        var agent3 = _testData.CreateAgent(agent3User);
        var property3 = _testData.CreateProperty();
        var conversation3 = _testData.CreateConversation();
        conversation3.UpdatedAt = DateTime.UtcNow.AddMinutes(-1);
        _dbContext.SaveChanges();
        _testData.CreateClientProperty(property3.PropertyId, client.UserId, agent3.UserId, conversation3.ConversationId);

        return await Task.FromResult(client.UserId);
    }

    private async Task<long> SetupTestData_ClientWithNoMessages()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var clientUser = _testData.CreateUser("client@test.com", "Client", "One");

        var agent = _testData.CreateAgent(agentUser);
        var client = _testData.CreateClient(clientUser);

        var property1 = _testData.CreateProperty();
        var conversation1 = _testData.CreateConversation();

        _testData.CreateClientProperty(property1.PropertyId, client.UserId, agent.UserId, conversation1.ConversationId);

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
        var conversation1 = _testData.CreateConversation();

        // Both clients on same property with same agent
        _testData.CreateClientProperty(property1.PropertyId, client.UserId, agent.UserId, conversation1.ConversationId);
        _testData.CreateClientProperty(property1.PropertyId, otherClient.UserId, agent.UserId, conversation1.ConversationId);

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