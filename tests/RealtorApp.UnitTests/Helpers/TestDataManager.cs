using RealtorApp.Domain.Models;

namespace RealtorApp.UnitTests.Helpers;

public class TestDataManager : IDisposable
{
    private readonly RealtorAppDbContext _dbContext;
    private readonly List<long> _createdUserIds = new();
    private readonly List<long> _createdAgentIds = new();
    private readonly List<long> _createdClientIds = new();
    private readonly List<long> _createdInvitationIds = new();
    private readonly List<long> _createdPropertyIds = new();
    private readonly List<long> _createdConversationIds = new();
    private readonly List<long> _createdMessageIds = new();
    private readonly List<long> _createdClientPropertyIds = new();
    private readonly List<long> _createdPropertyInvitationIds = new();
    private readonly List<long> _createdClientInvitationsPropertyIds = new();

    private long _nextUserId = new Random().Next(999, 999999); // Start from a unique base

    public TestDataManager(RealtorAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public User CreateUser(string email, string firstName, string lastName, Guid? uuid = null)
    {
        var userId = _nextUserId++;
        var user = new User
        {
            UserId = userId,
            Uuid = uuid ?? Guid.NewGuid(),
            Email = Guid.NewGuid().ToString() + email,
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();
        _createdUserIds.Add(userId);
        return user;
    }

    public Agent CreateAgent(User user)
    {
        var agent = new Agent
        {
            UserId = user.UserId,
            User = user,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Agents.Add(agent);
        _dbContext.SaveChanges();
        _createdAgentIds.Add(user.UserId);
        return agent;
    }

    public Client CreateClient(User user)
    {
        var client = new Client
        {
            UserId = user.UserId,
            User = user,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Clients.Add(client);
        _dbContext.SaveChanges();
        _createdClientIds.Add(user.UserId);
        return client;
    }

    public ClientInvitation CreateClientInvitation(long agentUserId, string? email = null,
        string? firstName = "John", string? lastName = "Doe", string? phone = "+1234567890",
        DateTime? expiresAt = null, DateTime? acceptedAt = null, DateTime? deletedAt = null)
    {
        var invitation = new ClientInvitation
        {
            ClientEmail = email ?? $"invitation{Guid.NewGuid():N}@example.com",
            ClientFirstName = firstName,
            ClientLastName = lastName,
            ClientPhone = phone,
            InvitationToken = Guid.NewGuid(),
            InvitedBy = agentUserId,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7),
            AcceptedAt = acceptedAt,
            DeletedAt = deletedAt,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.ClientInvitations.Add(invitation);
        _dbContext.SaveChanges();
        _createdInvitationIds.Add(invitation.ClientInvitationId);
        return invitation;
    }

    public Property CreateProperty(string addressLine1 = "123 Test Street", string city = "Test City",
        string region = "Test Region", string postalCode = "12345", string countryCode = "US")
    {
        var propertyId = _nextUserId++; // Reuse the ID generator
        var property = new Property
        {
            PropertyId = propertyId,
            AddressLine1 = addressLine1,
            City = city,
            Region = region,
            PostalCode = postalCode,
            CountryCode = countryCode,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Properties.Add(property);
        _dbContext.SaveChanges();
        _createdPropertyIds.Add(propertyId);
        return property;
    }

    public Conversation CreateConversation(DateTime? createdAt = null, DateTime? updatedAt = null)
    {
        var conversationId = _nextUserId++;
        var conversation = new Conversation
        {
            ConversationId = conversationId,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            UpdatedAt = updatedAt ?? DateTime.UtcNow
        };
        _dbContext.Conversations.Add(conversation);
        _dbContext.SaveChanges();
        _createdConversationIds.Add(conversationId);
        return conversation;
    }

    public Message CreateMessage(long conversationId, long senderId, string text, DateTime? createdAt = null, bool isRead = true)
    {
        var messageId = _nextUserId++;
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
        _dbContext.SaveChanges();
        _createdMessageIds.Add(messageId);
        return message;
    }

    public ClientsProperty CreateClientProperty(long propertyId, long clientId, long agentId, long conversationId, string title = "Test Property")
    {
        var clientPropertyId = _nextUserId++;
        var clientProperty = new ClientsProperty
        {
            ClientPropertyId = clientPropertyId,
            PropertyId = propertyId,
            ClientId = clientId,
            AgentId = agentId,
            ConversationId = conversationId,
            Title = title,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.ClientsProperties.Add(clientProperty);
        _dbContext.SaveChanges();
        _createdClientPropertyIds.Add(clientPropertyId);
        return clientProperty;
    }

    public PropertyInvitation CreatePropertyInvitation(string addressLine1, string city, string region,
        string postalCode, string countryCode, long invitedBy)
    {
        var propertyInvitationId = _nextUserId++;
        var propertyInvitation = new PropertyInvitation
        {
            PropertyInvitationId = propertyInvitationId,
            AddressLine1 = addressLine1,
            City = city,
            Region = region,
            PostalCode = postalCode,
            CountryCode = countryCode,
            InvitedBy = invitedBy,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.PropertyInvitations.Add(propertyInvitation);
        _dbContext.SaveChanges();
        _createdPropertyInvitationIds.Add(propertyInvitationId);
        return propertyInvitation;
    }

    public ClientInvitationsProperty CreateClientInvitationsProperty(long clientInvitationId, long propertyInvitationId)
    {
        var id = _nextUserId++;
        var clientInvitationsProperty = new ClientInvitationsProperty
        {
            ClientInvitationPropertyId = id,
            PropertyInvitationId = propertyInvitationId,
            ClientInvitationId = clientInvitationId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.ClientInvitationsProperties.Add(clientInvitationsProperty);
        _dbContext.SaveChanges();
        _createdClientInvitationsPropertyIds.Add(id);
        return clientInvitationsProperty;
    }

    public void Dispose()
    {
        try
        {
            // Delete in correct order to avoid foreign key constraints
            if (_createdMessageIds.Any())
            {
                _dbContext.Messages.RemoveRange(_dbContext.Messages.Where(m => _createdMessageIds.Contains(m.MessageId)));
            }
            if (_createdClientPropertyIds.Any())
            {
                _dbContext.ClientsProperties.RemoveRange(_dbContext.ClientsProperties.Where(cp => _createdClientPropertyIds.Contains(cp.ClientPropertyId)));
            }
            if (_createdClientInvitationsPropertyIds.Any())
            {
                _dbContext.ClientInvitationsProperties.RemoveRange(_dbContext.ClientInvitationsProperties.Where(cip => _createdClientInvitationsPropertyIds.Contains(cip.ClientInvitationPropertyId)));
            }
            if (_createdPropertyInvitationIds.Any())
            {
                _dbContext.PropertyInvitations.RemoveRange(_dbContext.PropertyInvitations.Where(pi => _createdPropertyInvitationIds.Contains(pi.PropertyInvitationId)));
            }
            if (_createdConversationIds.Any())
            {
                _dbContext.Conversations.RemoveRange(_dbContext.Conversations.Where(c => _createdConversationIds.Contains(c.ConversationId)));
            }
            if (_createdPropertyIds.Any())
            {
                _dbContext.Properties.RemoveRange(_dbContext.Properties.Where(p => _createdPropertyIds.Contains(p.PropertyId)));
            }
            if (_createdInvitationIds.Any())
            {
                _dbContext.ClientInvitations.RemoveRange(_dbContext.ClientInvitations.Where(ci => _createdInvitationIds.Contains(ci.ClientInvitationId)));
            }
            if (_createdClientIds.Any())
            {
                _dbContext.Clients.RemoveRange(_dbContext.Clients.Where(c => _createdClientIds.Contains(c.UserId)));
            }
            if (_createdAgentIds.Any())
            {
                _dbContext.Agents.RemoveRange(_dbContext.Agents.Where(a => _createdAgentIds.Contains(a.UserId)));
            }
            if (_createdUserIds.Any())
            {
                _dbContext.Users.RemoveRange(_dbContext.Users.Where(u => _createdUserIds.Contains(u.UserId)));
            }

            _dbContext.SaveChanges();
        }
        catch (Exception)
        {
            // Ignore cleanup errors in tests
        }
    }
}