using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using RealtorApp.Contracts.Queries.Chat.Requests;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Models;
using RealtorApp.Domain.Services;
using Task = System.Threading.Tasks.Task;

namespace RealtorApp.UnitTests.Services;

public class ChatServiceTests : IDisposable
{
    private readonly RealtorAppDbContext _dbContext;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<IUserAuthService> _mockUserAuthService;
    private readonly ChatService _chatService;

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

        _mockCache = new Mock<IMemoryCache>();
        _mockUserAuthService = new Mock<IUserAuthService>();

        _chatService = new ChatService(_dbContext, _mockCache.Object, _mockUserAuthService.Object);

        // Clean up any existing test data
        CleanupTestData();
    }

    [Fact]
    public async Task GetAgentConversationListAsync_WithMultipleClientsOnSameProperty_GroupsThemTogether()
    {
        // Arrange
        var agentId = 1L;
        await SetupTestData_MultipleClientsOneProperty(agentId);

        var query = new GetConversationListQuery { Limit = 10, Offset = 0 };

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
        var agentId = 1L;
        await SetupTestData_SameClientsDifferentProperties(agentId);

        var query = new GetConversationListQuery { Limit = 10, Offset = 0 };

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
        var agentId = 1L;
        await SetupTestData_WithMessages(agentId);

        var query = new GetConversationListQuery { Limit = 10, Offset = 0 };

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
        var agentId = 1L;
        await SetupTestData_WithUnreadMessages(agentId);

        var query = new GetConversationListQuery { Limit = 10, Offset = 0 };

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
        var agentId = 1L;
        await SetupTestData_MultipleSeparateGroups(agentId);

        var query = new GetConversationListQuery { Limit = 2, Offset = 0 };

        // Act
        var result = await _chatService.GetAgentConversationListAsync(query, agentId);

        // Assert
        Assert.NotNull(result);
        // Assert.Null(result.ErrorMessage);
        Assert.Equal(2, result.Conversations.Count); // Should return 2 items
        Assert.True(result.TotalCount >= 3); // Should indicate more items exist
        Assert.True(result.HasMore);
    }

    private async Task SetupTestData_MultipleClientsOneProperty(long agentId)
    {
        // Create users first
        var agentUser = CreateUser(agentId, "agent@test.com", "Agent", "One");
        var client1User = CreateUser(2L, "client1@test.com", "Client", "One");
        var client2User = CreateUser(3L, "client2@test.com", "Client", "Two");
        await _dbContext.SaveChangesAsync();

        // Create agent and clients
        var agent = CreateAgent(agentId, agentUser);
        var client1 = CreateClient(2L, client1User);
        var client2 = CreateClient(3L, client2User);
        await _dbContext.SaveChangesAsync();

        // Create property
        var property1 = CreateProperty(1L);
        await _dbContext.SaveChangesAsync();

        // Create conversation
        var conversation = CreateConversation(1L);
        await _dbContext.SaveChangesAsync();

        // Create client properties (both clients on same property)
        CreateClientProperty(1L, 1L, 2L, agentId, conversation.ConversationId); // Property 1, Client 1
        CreateClientProperty(2L, 1L, 3L, agentId, conversation.ConversationId); // Property 1, Client 2
        await _dbContext.SaveChangesAsync();
    }

    private async Task SetupTestData_SameClientsDifferentProperties(long agentId)
    {
        // Create users
        var agentUser = CreateUser(agentId, "agent@test.com", "Agent", "One");
        var client1User = CreateUser(2L, "client1@test.com", "Client", "One");
        var client2User = CreateUser(3L, "client2@test.com", "Client", "Two");

        var agent = CreateAgent(agentId, agentUser);
        var client1 = CreateClient(2L, client1User);
        var client2 = CreateClient(3L, client2User);

        // Create properties
        var property1 = CreateProperty(1L);
        var property2 = CreateProperty(2L);

        // Create conversations
        var conversation1 = CreateConversation(1L);
        var conversation2 = CreateConversation(2L);

        // Create client properties (same clients on different properties)
        CreateClientProperty(1L, 1L, 2L, agentId, conversation1.ConversationId); // Property 1, Client 1
        CreateClientProperty(2L, 1L, 3L, agentId, conversation1.ConversationId); // Property 1, Client 2
        CreateClientProperty(3L, 2L, 2L, agentId, conversation2.ConversationId); // Property 2, Client 1
        CreateClientProperty(4L, 2L, 3L, agentId, conversation2.ConversationId); // Property 2, Client 2

        await _dbContext.SaveChangesAsync();
    }

    private async Task SetupTestData_WithMessages(long agentId)
    {
        await SetupTestData_MultipleClientsOneProperty(agentId);

        // Add messages with different timestamps
        CreateMessage(1L, 1L, 2L, "Older message", DateTime.UtcNow.AddMinutes(-10));
        CreateMessage(2L, 1L, 3L, "Most recent message", DateTime.UtcNow.AddMinutes(-1));

        await _dbContext.SaveChangesAsync();
    }

    private async Task SetupTestData_WithUnreadMessages(long agentId)
    {
        await SetupTestData_SameClientsDifferentProperties(agentId);

        // Add unread messages in both conversations
        CreateMessage(1L, 1L, 2L, "Unread message 1", DateTime.UtcNow.AddMinutes(-10), isRead: false);
        CreateMessage(2L, 2L, 3L, "Unread message 2", DateTime.UtcNow.AddMinutes(-5), isRead: false);
        CreateMessage(3L, 1L, agentId, "Read message from agent", DateTime.UtcNow.AddMinutes(-3), isRead: true);

        await _dbContext.SaveChangesAsync();
    }

    private async Task SetupTestData_MultipleSeparateGroups(long agentId)
    {
        // Create multiple distinct client groups
        var agentUser = CreateUser(agentId, "agent@test.com", "Agent", "One");
        var agent = CreateAgent(agentId, agentUser);

        // Create properties
        var property1 = CreateProperty(1L);
        var property2 = CreateProperty(2L);
        var property3 = CreateProperty(3L);

        // Group 1: Client 1 & 2 on Property 1
        var client1User = CreateUser(2L, "client1@test.com", "Client", "One");
        var client2User = CreateUser(3L, "client2@test.com", "Client", "Two");
        var client1 = CreateClient(2L, client1User);
        var client2 = CreateClient(3L, client2User);
        var conversation1 = CreateConversation(1L);
        CreateClientProperty(1L, 1L, 2L, agentId, conversation1.ConversationId);
        CreateClientProperty(2L, 1L, 3L, agentId, conversation1.ConversationId);

        // Group 2: Client 3 on Property 2
        var client3User = CreateUser(4L, "client3@test.com", "Client", "Three");
        var client3 = CreateClient(4L, client3User);
        var conversation2 = CreateConversation(2L);
        CreateClientProperty(3L, 2L, 4L, agentId, conversation2.ConversationId);

        // Group 3: Client 4 & 5 on Property 3
        var client4User = CreateUser(5L, "client4@test.com", "Client", "Four");
        var client5User = CreateUser(6L, "client5@test.com", "Client", "Five");
        var client4 = CreateClient(5L, client4User);
        var client5 = CreateClient(6L, client5User);
        var conversation3 = CreateConversation(3L);
        CreateClientProperty(4L, 3L, 5L, agentId, conversation3.ConversationId);
        CreateClientProperty(5L, 3L, 6L, agentId, conversation3.ConversationId);

        await _dbContext.SaveChangesAsync();
    }

    private void CleanupTestData()
    {
        // Delete in correct order to avoid foreign key constraints
        _dbContext.Messages.RemoveRange(_dbContext.Messages.Where(m => m.MessageId <= 100));
        _dbContext.ClientsProperties.RemoveRange(_dbContext.ClientsProperties.Where(cp => cp.ClientPropertyId <= 100));
        _dbContext.Conversations.RemoveRange(_dbContext.Conversations.Where(c => c.ConversationId <= 100));
        _dbContext.Properties.RemoveRange(_dbContext.Properties.Where(p => p.PropertyId <= 100));
        _dbContext.Clients.RemoveRange(_dbContext.Clients.Where(c => c.UserId <= 100));
        _dbContext.Agents.RemoveRange(_dbContext.Agents.Where(a => a.UserId <= 100));
        _dbContext.Users.RemoveRange(_dbContext.Users.Where(u => u.UserId <= 100));
        _dbContext.SaveChanges();
    }

    #region Helper Methods

    private User CreateUser(long userId, string email, string firstName, string lastName)
    {
        var user = new User
        {
            UserId = userId,
            Uuid = Guid.NewGuid(),
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Users.Add(user);
        return user;
    }

    private Agent CreateAgent(long userId, User user)
    {
        var agent = new Agent
        {
            UserId = userId,
            User = user,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Agents.Add(agent);
        return agent;
    }

    private Client CreateClient(long userId, User user)
    {
        var client = new Client
        {
            UserId = userId,
            User = user,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Clients.Add(client);
        return client;
    }

    private Conversation CreateConversation(long conversationId)
    {
        var conversation = new Conversation
        {
            ConversationId = conversationId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Conversations.Add(conversation);
        return conversation;
    }

    private ClientsProperty CreateClientProperty(long clientPropertyId, long propertyId, long clientId, long agentId, long conversationId)
    {
        var clientProperty = new ClientsProperty
        {
            ClientPropertyId = clientPropertyId,
            PropertyId = propertyId,
            ClientId = clientId,
            AgentId = agentId,
            ConversationId = conversationId,
            Title = $"Property {propertyId}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.ClientsProperties.Add(clientProperty);
        return clientProperty;
    }

    private Property CreateProperty(long propertyId)
    {
        var property = new Property
        {
            PropertyId = propertyId,
            AddressLine1 = $"123 Test Street {propertyId}",
            City = "Test City",
            Region = "Test Region",
            PostalCode = "12345",
            CountryCode = "US",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Properties.Add(property);
        return property;
    }

    private Message CreateMessage(long messageId, long conversationId, long senderId, string text, DateTime? createdAt = null, bool isRead = true)
    {
        var now = createdAt ?? DateTime.UtcNow;
        var message = new Message
        {
            MessageId = messageId,
            ConversationId = conversationId,
            SenderId = senderId,
            MessageText = text,
            IsRead = isRead,
            CreatedAt = now,
            UpdatedAt = now
        };
        _dbContext.Messages.Add(message);
        return message;
    }

    #endregion

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}