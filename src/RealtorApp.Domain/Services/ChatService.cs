using Microsoft.EntityFrameworkCore;
using RealtorApp.Contracts.Commands.Chat.Requests;
using RealtorApp.Contracts.Commands.Chat.Responses;
using RealtorApp.Contracts.Queries.Chat.Requests;
using RealtorApp.Contracts.Queries.Chat.Responses;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Extensions;
using RealtorApp.Infra.Data;
using Microsoft.Extensions.Logging;
using RealtorApp.Contracts.Enums;
using Task = System.Threading.Tasks.Task;

namespace RealtorApp.Domain.Services;

public class ChatService(RealtorAppDbContext context, ILogger<ChatService> logger) : IChatService
{
    private readonly RealtorAppDbContext _context = context;
    private readonly ILogger<ChatService> _logger = logger;

    public async Task<SendMessageCommandResponse> SendMessageAsync(SendMessageCommand command, HashSet<long> userIds)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var message = command.ToDbModel();

            if (command.AttachmentRequests.Length > 0)
            {
                var attachments = command.AttachmentRequests.Select((ar) => {
                    var attachment = new Attachment
                    {
                        MessageId = message.MessageId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow

                    };

                    if (ar.Type == AttachmentType.Task)
                    {
                        attachment.TaskAttachment = new()
                        {
                          TaskId = ar.AttachmentObjectId,
                          CreatedAt = DateTime.UtcNow,
                          UpdatedAt = DateTime.UtcNow
                        };
                    }

                    if (ar.Type == AttachmentType.Contact)
                    {
                        attachment.ContactAttachment = new()
                        {
                          ThirdPartyContactId = ar.AttachmentObjectId,
                          CreatedAt = DateTime.UtcNow,
                          UpdatedAt = DateTime.UtcNow
                        };
                    }

                    return attachment;
                });

                message.Attachments = [.. attachments];
            }

            await _context.Messages.AddAsync(message);

            await MarkMessageAsReadByUsersAsync(message, userIds);

            await _context.SaveChangesAsync();

            await _context.Conversations.Where(i => i.ListingId == command.ConversationId)
                .ExecuteUpdateAsync(i => i.SetProperty(x => x.UpdatedAt, DateTime.UtcNow));

            await transaction.CommitAsync();

            var senderDetails = await _context.Users.FindAsync(message.SenderId);

            message.Attachments = await _context.Attachments
                .Where(i => i.MessageId == message.MessageId)
                .Select(i => new Attachment()
                {
                    TaskAttachment = i.TaskAttachment == null ? null : new()
                    {
                        TaskId = i.TaskAttachment.Task.TaskId,
                        Task = new()
                        {
                            Title = i.TaskAttachment.Task.Title
                        }
                    },
                    ContactAttachment = i.ContactAttachment == null ? null : new()
                    {
                        ThirdPartyContactId = i.ContactAttachment.ThirdPartyContact.ThirdPartyContactId,
                        ThirdPartyContact = new()
                        {
                            Name = i.ContactAttachment.ThirdPartyContact.Name,
                        }
                    }
                })
                .ToListAsync();

            if (senderDetails != null)
            {
                message.Sender = senderDetails;
            }

            return message.ToSendMessageResponse();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed saving message - {Message}", ex.Message);
            return new SendMessageCommandResponse { ErrorMessage = "Failed to send message" };
        }
    }

    public async Task<MarkMessagesAsReadCommandResponse> MarkMessagesAsReadAsync(MarkMessagesAsReadCommand command, long userId)
    {
        try
        {
            var messages = await _context.Messages
                .Where(m => command.MessageIds.Contains(m.MessageId))
                .ToListAsync();

            var messageReads = new List<MessageRead>();

            foreach (var message in messages)
            {
                var messageRead = new MessageRead()
                {
                    MessageId = message.MessageId,
                    ReaderId = userId,
                };

                messageReads.Add(messageRead);
            }

            await _context.MessageReads.AddRangeAsync(messageReads);

            await _context.SaveChangesAsync();

            return new MarkMessagesAsReadCommandResponse
            {
                MarkedMessageIds = [.. command.MessageIds],
                TotalMarkedCount = messages.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking messages as read - {Message}", ex.Message);
            return new MarkMessagesAsReadCommandResponse { ErrorMessage = "Failed to mark messages as read" };
        }
    }

    public async Task MarkMessageAsReadByUsersAsync(Message message, HashSet<long> userIds)
    {
        List<MessageRead> messageReads = [];

        foreach (var userId in userIds)
        {
            var messageRead = new MessageRead()
            {
              Message = message,
              ReaderId = userId,
              CreatedAt = DateTime.UtcNow,
              UpdatedAt = DateTime.UtcNow,
            };

            messageReads.Add(messageRead);
        }

        await _context.MessageReads.AddRangeAsync(messageReads);
    }

    public async Task<MessageHistoryQueryResponse> GetMessageHistoryAsync(MessageHistoryQuery query, long userId, long conversationId)
    {
        try
        {
            var messagesQuery = _context.Messages
                .Include(i => i.Sender)
                .Where(m => m.ConversationId == conversationId)
                .AsNoTracking();

            if (query.Before.HasValue)
            {
                messagesQuery = messagesQuery.Where(m => m.CreatedAt < query.Before.Value);
            }

            var limit = Math.Min(query.Limit, 100);
            var messages = await messagesQuery
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit + 1)
                .Include(m => m.Attachments)
                    .ThenInclude(a => a.TaskAttachment)
                        .ThenInclude(i => i!.Task)
                .Include(m => m.Attachments)
                    .ThenInclude(a => a.ContactAttachment)
                        .ThenInclude(i => i!.ThirdPartyContact)
                .ToListAsync();

            var hasMore = messages.Count > limit;
            if (hasMore)
            {
                messages.RemoveAt(messages.Count - 1); // Remove the extra message
            }

            var messageResponses = messages.Select(m => m.ToMessageResponse()).GroupMessagesByDate();
            var nextBefore = hasMore && messages.Count > 0 ? messages.Last().CreatedAt : (DateTime?)null;

            return new MessageHistoryQueryResponse
            {
                MessageGroups = messageResponses,
                HasMore = hasMore,
                NextBefore = nextBefore
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed loading message history - {Message}", ex.Message);
            return new MessageHistoryQueryResponse { ErrorMessage = "Failed to retrieve message history" };
        }
    }

    public async Task<ConversationListQueryResponse> GetClientConversationList(ConversationListQuery query, long clientId)
    {
        try
        {
            var clientListings = await _context.Conversations
                .Include(i => i.Messages)
                    .ThenInclude(i => i.Sender)
                .Where(i => i.Listing.ClientsListings.Any(i => i.ClientId == clientId && i.DeletedAt == null))
                .Skip(query.Offset)
                .Take(query.Limit)
                .Select(i => new
                {
                    AgentUsers = i.Listing.AgentsListings.Select(al => al.Agent.User),
                    i.Listing.Conversation,
                    Address = i.Listing.Property.AddressLine1 + " " + i.Listing.Property.AddressLine2,
                    LastMessage = i.Messages.OrderByDescending(i => i.CreatedAt).FirstOrDefault(),
                    LatestMessageIsReadByUser = i.Messages.OrderByDescending(i => i.CreatedAt).Take(1).Any(i => i.MessageReads.Any(x => x.ReaderId == clientId))

                }).ToListAsync();

            var totalCount = await _context.ClientsListings.Where(i => i.ClientId == clientId).CountAsync();

            var conversations = new List<ConversationResponse>();

            foreach (var clientListing in clientListings)
            {
                var conversation = new ConversationResponse()
                {
                    OtherUsers = [.. clientListing.AgentUsers.Select(i => new UserDetailsConversationResponse()
                        {
                            Name = i.FirstName + " " + i.LastName,
                            UserId = i.UserId
                        })],
                    Address = clientListing.Address,
                    ConversationUpdatedAt = clientListing.Conversation!.UpdatedAt,
                    ConversationId = clientListing.Conversation.ListingId,
                    LastMessage = clientListing.LastMessage?.ToMessageResponse(),
                    HasUnreadMessage = clientListing.LastMessage != null && !clientListing.LatestMessageIsReadByUser
                };

                conversations.Add(conversation);
            }

            return new ConversationListQueryResponse()
            {
                HasMore = query.Offset + conversations.Count < totalCount,
                Conversations = [.. conversations.OrderByDescending(i => i.ConversationUpdatedAt) ],
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed loading convo list for client - {Message}", ex.Message);
            return new ConversationListQueryResponse { ErrorMessage = "Failed to retrieve conversations" };
        }
    }

    public async Task<ConversationListQueryResponse> GetAgentConversationListAsync(ConversationListQuery query, long agentId)
    {
        try
        {
            var convosList = await _context.Conversations
                .Include(i => i.Messages)
                    .ThenInclude(i => i.Sender)
                .Where(i => i.Listing.AgentsListings.Any(i => i.AgentId == agentId && i.DeletedAt == null))
                .Skip(query.Offset)
                .Take(query.Limit)
                .Select(i => new
                {
                    ClientIds = i.Listing.ClientsListings.Select(i => i.ClientId).OrderByDescending(i => i).ToList(),
                    ClientData = i.Listing.ClientsListings.Select(x => new { x.ClientId, ClientName = x.Client.User.FirstName + " " + x.Client.User.LastName }).ToList(),
                    Conversation = i,
                    Address = i.Listing.Property.AddressLine1 + " " + i.Listing.Property.AddressLine2,
                    LastMessage = i.Messages.OrderByDescending(i => i.CreatedAt).FirstOrDefault(),
                    LatestMessageIsReadByUser = i.Messages.OrderByDescending(i => i.CreatedAt).Take(1).Any(i => i.MessageReads.Any(x => x.ReaderId == agentId))
                })
                .ToListAsync();

            var totalCount = await _context.Conversations
                .Where(i => i.Listing.AgentsListings.Any(i => i.AgentId == agentId && i.DeletedAt == null)).CountAsync();

            var mappedConvos = new List<ConversationResponse>();

            foreach (var convo in convosList)
            {
                var conversation = new ConversationResponse()
                {
                    ConversationUpdatedAt = convo.Conversation.UpdatedAt,
                    ConversationId = convo.Conversation.ListingId,
                    Address = convo.Address,
                    OtherUsers = convo.ClientData?
                        .Select(i => new UserDetailsConversationResponse() { UserId = i.ClientId, Name = i.ClientName })
                        .ToArray() ?? [],
                    LastMessage = convo.LastMessage?.ToMessageResponse(),
                    HasUnreadMessage = convo.LastMessage != null && !convo.LatestMessageIsReadByUser
                };

                mappedConvos.Add(conversation);
            }

            var hasMore = query.Offset + mappedConvos.Count < totalCount;

            return new ConversationListQueryResponse
            {
                Conversations = [.. mappedConvos.OrderByDescending(i => i.ConversationUpdatedAt)],
                TotalCount = totalCount,
                HasMore = hasMore
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed loading convo list for agent - {Message}", ex.Message);
            return new ConversationListQueryResponse { ErrorMessage = "Failed to retrieve conversations" };
        }
    }
}
