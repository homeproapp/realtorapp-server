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
    private readonly List<long> _createdListingIds = new();
    private readonly List<long> _createdMessageIds = new();
    private readonly List<long> _createdMessageReadIds = new();
    private readonly List<long> _createdClientListingIds = new();
    private readonly List<long> _createdAgentListingIds = new();
    private readonly List<long> _createdPropertyInvitationIds = new();
    private readonly List<long> _createdClientInvitationsPropertyIds = new();
    private readonly List<long> _createdTaskIds = new();

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

    public Listing CreateListing(long propertyId, string? title = "Test Listing", DateTime? createdAt = null, DateTime? updatedAt = null)
    {
        var listingId = _nextUserId++;
        var listing = new Listing
        {
            ListingId = listingId,
            PropertyId = propertyId,
            Title = title,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            UpdatedAt = updatedAt ?? DateTime.UtcNow
        };
        _dbContext.Listings.Add(listing);
        _dbContext.SaveChanges();
        _createdListingIds.Add(listingId);
        return listing;
    }

    public Conversation CreateConversation(long listingId, DateTime? createdAt = null, DateTime? updatedAt = null)
    {
        var conversation = new Conversation
        {
            ListingId = listingId,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            UpdatedAt = updatedAt ?? DateTime.UtcNow
        };
        _dbContext.Conversations.Add(conversation);
        _dbContext.SaveChanges();
        return conversation;
    }

    public Message CreateMessage(long listingId, long senderId, string text, DateTime? createdAt = null)
    {
        var messageId = _nextUserId++;
        var now = createdAt ?? DateTime.UtcNow;
        var message = new Message
        {
            MessageId = messageId,
            ConversationId = listingId,
            SenderId = senderId,
            MessageText = text,
            CreatedAt = now,
            UpdatedAt = now
        };
        _dbContext.Messages.Add(message);
        _dbContext.SaveChanges();
        _createdMessageIds.Add(messageId);
        return message;
    }

    public MessageRead CreateMessageRead(long messageId, long readerId, DateTime? createdAt = null)
    {
        var messageReadId = _nextUserId++;
        var now = createdAt ?? DateTime.UtcNow;
        var messageRead = new MessageRead
        {
            MessageReadId = messageReadId,
            MessageId = messageId,
            ReaderId = readerId,
            CreatedAt = now,
            UpdatedAt = now
        };
        _dbContext.MessageReads.Add(messageRead);
        _dbContext.SaveChanges();
        _createdMessageReadIds.Add(messageReadId);
        return messageRead;
    }

    public ClientsListing CreateClientListing(long listingId, long clientId)
    {
        var clientListingId = _nextUserId++;
        var clientListing = new ClientsListing
        {
            ClientListingId = clientListingId,
            ListingId = listingId,
            ClientId = clientId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.ClientsListings.Add(clientListing);
        _dbContext.SaveChanges();
        _createdClientListingIds.Add(clientListingId);
        return clientListing;
    }

    public AgentsListing CreateAgentListing(long listingId, long agentId)
    {
        var agentListingId = _nextUserId++;
        var agentListing = new AgentsListing
        {
            AgentListingId = agentListingId,
            ListingId = listingId,
            AgentId = agentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.AgentsListings.Add(agentListing);
        _dbContext.SaveChanges();
        _createdAgentListingIds.Add(agentListingId);
        return agentListing;
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

    public RealtorApp.Domain.Models.Task CreateTask(long listingId, string? title = "Test Task", short? status = null, DateTime? updatedAt = null)
    {
        var taskId = _nextUserId++;
        var task = new RealtorApp.Domain.Models.Task
        {
            TaskId = taskId,
            ListingId = listingId,
            Title = title,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = updatedAt ?? DateTime.UtcNow
        };
        _dbContext.Tasks.Add(task);
        _dbContext.SaveChanges();
        _createdTaskIds.Add(taskId);
        return task;
    }

    public void Dispose()
    {
        try
        {
            if (_createdTaskIds.Any())
            {
                _dbContext.Tasks.RemoveRange(_dbContext.Tasks.Where(t => _createdTaskIds.Contains(t.TaskId)));
            }
            if (_createdMessageReadIds.Any())
            {
                _dbContext.MessageReads.RemoveRange(_dbContext.MessageReads.Where(mr => _createdMessageReadIds.Contains(mr.MessageReadId)));
            }
            if (_createdMessageIds.Any())
            {
                _dbContext.Messages.RemoveRange(_dbContext.Messages.Where(m => _createdMessageIds.Contains(m.MessageId)));
            }
            if (_createdClientInvitationsPropertyIds.Any())
            {
                _dbContext.ClientInvitationsProperties.RemoveRange(_dbContext.ClientInvitationsProperties.Where(cip => _createdClientInvitationsPropertyIds.Contains(cip.ClientInvitationPropertyId)));
            }
            if (_createdPropertyInvitationIds.Any())
            {
                _dbContext.PropertyInvitations.RemoveRange(_dbContext.PropertyInvitations.Where(pi => _createdPropertyInvitationIds.Contains(pi.PropertyInvitationId)));
            }
            if (_createdListingIds.Any())
            {
                var listings = _dbContext.Listings.Where(l => _createdListingIds.Contains(l.ListingId)).ToList();
                var conversationsToRemove = _dbContext.Conversations.Where(c => _createdListingIds.Contains(c.ListingId)).ToList();
                _dbContext.Conversations.RemoveRange(conversationsToRemove);
                _dbContext.Listings.RemoveRange(listings);
            }
            if (_createdClientListingIds.Any())
            {
                _dbContext.ClientsListings.RemoveRange(_dbContext.ClientsListings.Where(cl => _createdClientListingIds.Contains(cl.ClientListingId)));
            }
            if (_createdAgentListingIds.Any())
            {
                _dbContext.AgentsListings.RemoveRange(_dbContext.AgentsListings.Where(al => _createdAgentListingIds.Contains(al.AgentListingId)));
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
        }
    }
}