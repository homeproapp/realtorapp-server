using Microsoft.EntityFrameworkCore;
using RealtorApp.Contracts.Commands.Chat.Requests;
using RealtorApp.Contracts.Commands.Chat.Responses;
using RealtorApp.Contracts.Queries.Chat.Requests;
using RealtorApp.Contracts.Queries.Chat.Responses;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Extensions;
using RealtorApp.Infra.Data;
using Microsoft.Extensions.Logging;

namespace RealtorApp.Domain.Services;

public class ChatService(RealtorAppDbContext context, IUserAuthService userAuthService, ILogger<ChatService> logger) : IChatService
{
    private readonly RealtorAppDbContext _context = context;
    private readonly IUserAuthService _userAuthService = userAuthService;
    private readonly ILogger<ChatService> _logger = logger;

    public async Task<SendMessageCommandResponse> SendMessageAsync(SendMessageCommand command)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (!await _userAuthService.IsConversationParticipant(command.SenderId, command.ConversationId))
            {
                return new SendMessageCommandResponse { ErrorMessage = "Access denied" };
            }

            var message = command.ToDbModel();

            if (command.AttachmentRequests.Length > 0)
            {
                var attachments = command.AttachmentRequests.Select(ar => new Attachment
                {
                    MessageId = message.MessageId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                message.Attachments = [.. attachments];
            }

            await _context.Messages.AddAsync(message);

            await _context.SaveChangesAsync();

            await _context.Conversations.Where(i => i.ListingId == command.ConversationId)
                .ExecuteUpdateAsync(i => i.SetProperty(x => x.UpdatedAt, DateTime.UtcNow));

            await transaction.CommitAsync();
            return message.ToSendMessageResponse();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed saving message - {Message}", ex.Message);
            return new SendMessageCommandResponse { ErrorMessage = "Failed to send message" };
        }
    }

    //TODO: marking message as read is using new table
    // public async Task<MarkMessagesAsReadCommandResponse> MarkMessagesAsReadAsync(MarkMessagesAsReadCommand command, long userId)
    // {
    //     try
    //     {
    //         var messages = await _context.Messages
    //             .Where(m => command.MessageIds.Contains(m.MessageId))
    //             .ToListAsync();

    //         var validMessageIds = new List<long>();

    //         foreach (var message in messages)
    //         {
    //             if (await _userAuthService.IsConversationParticipant(userId,message.ConversationId))
    //             {
    //                 message.UpdatedAt = DateTime.UtcNow;
    //                 validMessageIds.Add(message.MessageId);
    //             }
    //         }

    //         await _context.SaveChangesAsync();

    //         return new MarkMessagesAsReadCommandResponse
    //         {
    //             MarkedMessageIds = validMessageIds.ToArray(),
    //             TotalMarkedCount = validMessageIds.Count
    //         };
    //     }
    //     catch (Exception ex)
    //     {
    //         return new MarkMessagesAsReadCommandResponse { ErrorMessage = "Failed to mark messages as read" };
    //     }
    // }

    public async Task<MessageHistoryQueryResponse> GetMessageHistoryAsync(MessageHistoryQuery query, long userId, long conversationId)
    {
        try
        {
            // Validate conversation participant
            if (!await _userAuthService.IsConversationParticipant(userId, conversationId))
            {
                return new MessageHistoryQueryResponse { ErrorMessage = "Access denied" };
            }

            var messagesQuery = _context.Messages
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
                .Include(m => m.Attachments)
                    .ThenInclude(a => a.ContactAttachment)
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
            var clientListingsQuery = await _context.ClientsListings.Where(i => i.ClientId == clientId)
                .Select(i => new
                {
                    AgentUsers = i.Listing.AgentsListings.Select(al => al.Agent.User),
                    i.Listing.Conversation,
                    LastMessage = i.Listing.Conversation!.Messages.OrderByDescending(i => i.CreatedAt).FirstOrDefault(),
                    LatestMessageIsReadByUser = i.Listing.Conversation.Messages.OrderByDescending(i => i.CreatedAt).Take(1).Any(i => i.MessageReads.Any(x => x.ReaderId == clientId))

                }).ToListAsync();

            var clientListingsGroupedByAgents = clientListingsQuery.GroupBy(i => string.Join('|', i.AgentUsers
                .OrderByDescending(i => i.UserId).Select(i => i.UserId)));
            var groupedAgentConvosCount = clientListingsGroupedByAgents.Count();
            var conversations = new List<ConversationResponse>();

            foreach (var group in clientListingsGroupedByAgents.Skip(query.Offset).Take(query.Limit))
            {
                var latestConversationGroup = group.OrderByDescending(i => i.Conversation?.UpdatedAt ?? DateTime.MinValue).FirstOrDefault();
                var unreadConvoCount = group.Where(i => !i.LatestMessageIsReadByUser).Count();

                if (latestConversationGroup == null || latestConversationGroup.Conversation == null) continue;

                var conversation = new ConversationResponse()
                {
                    OtherUsers = [.. latestConversationGroup.AgentUsers.Select(i => new UserDetailsConversationResponse()
                        {
                            Name = i.FirstName + " " + i.LastName,
                            UserId = i.UserId
                        })],
                    ConversationUpdatedAt = latestConversationGroup.Conversation.UpdatedAt,
                    ClickThroughConversationId = latestConversationGroup.Conversation.ListingId,
                    LastMessage = latestConversationGroup.LastMessage?.ToMessageResponse(),
                    UnreadConversationCount = (byte)unreadConvoCount
                };

                conversations.Add(conversation);
            }

            return new ConversationListQueryResponse()
            {
                HasMore = query.Offset + conversations.Count < groupedAgentConvosCount,
                Conversations = [.. conversations.OrderByDescending(i => i.ConversationUpdatedAt) ],
                TotalCount = groupedAgentConvosCount
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
                .Where(i => i.Listing.AgentsListings.Any(i => i.AgentId == agentId && i.DeletedAt == null))
                .Select(i => new
                {
                    ClientIds = i.Listing.ClientsListings.Select(i => i.ClientId).OrderByDescending(i => i).ToList(),
                    ClientData = i.Listing.ClientsListings.Select(x => new { x.ClientId, ClientName = x.Client.User.FirstName + " " + x.Client.User.LastName }).ToList(),
                    Conversation = i,
                    LastMessage = i.Messages.OrderByDescending(i => i.CreatedAt).FirstOrDefault(),
                    LatestMessageIsReadByUser = i.Messages.OrderByDescending(i => i.CreatedAt).Take(1).Any(i => i.MessageReads.Any(x => x.ReaderId == agentId))
                })
                .ToListAsync();

            var convosGrouped = convosList.GroupBy(i => string.Join(",", i.ClientIds.OrderByDescending(i => i)), x => x);

            var count = convosGrouped.Count();

            var convosGroupedByClients = new List<ConversationResponse>();

            foreach (var group in convosGrouped.Skip(query.Offset).Take(query.Limit))
            {
                var latestConversation = group.OrderByDescending(i => i.Conversation.UpdatedAt).FirstOrDefault();
                var unreadConvoCount = group.Where(i => !i.LatestMessageIsReadByUser).Count();
                // group.Where(i => i.Conversation.Messages?.Any(i => !i.IsRead ?? false) ?? false).Count();

                if (latestConversation == null || latestConversation.Conversation == null) continue;

                var conversation = new ConversationResponse()
                {
                    ConversationUpdatedAt = latestConversation.Conversation.UpdatedAt,
                    ClickThroughConversationId = latestConversation.Conversation.ListingId,
                    OtherUsers = group.FirstOrDefault()?.ClientData?
                        .Select(i => new UserDetailsConversationResponse() { UserId = i.ClientId, Name = i.ClientName })
                        .ToArray() ?? [],
                    LastMessage = latestConversation.LastMessage?.ToMessageResponse(),
                    UnreadConversationCount = (byte)unreadConvoCount
                };

                convosGroupedByClients.Add(conversation);
            }

            var hasMore = query.Offset + convosGroupedByClients.Count < count;

            return new ConversationListQueryResponse
            {
                Conversations = [.. convosGroupedByClients.OrderByDescending(i => i.ConversationUpdatedAt)],
                TotalCount = count,
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
