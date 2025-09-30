using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RealtorApp.Contracts.Commands.Chat.Requests;
using RealtorApp.Contracts.Commands.Chat.Responses;
using RealtorApp.Contracts.Queries.Chat.Requests;
using RealtorApp.Contracts.Queries.Chat.Responses;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Extensions;
using RealtorApp.Domain.Models;
using RealtorApp.Domain.Comparers;
using RealtorApp.Domain.DTOs;

namespace RealtorApp.Domain.Services;

public class ChatService(RealtorAppDbContext context, IMemoryCache cache, IUserAuthService userAuthService, ISqlQueryService sqlQueryService) : IChatService
{
    private readonly RealtorAppDbContext _context = context;
    private readonly IMemoryCache _cache = cache;
    private readonly IUserAuthService _userAuthService = userAuthService;
    private readonly ISqlQueryService _sqlQueryService = sqlQueryService;

    public async Task<SendMessageCommandResponse> SendMessageAsync(SendMessageCommand command)
    {
        try
        {
            if (!await _userAuthService.IsConversationParticipant(command.ConversationId, command.SenderId))
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

            await _context.Conversations.Where(i => i.ConversationId == command.ConversationId)
                .ExecuteUpdateAsync(i => i.SetProperty(x => x.UpdatedAt, DateTime.UtcNow));

            return message.ToSendMessageResponse();
        }
        catch (Exception)
        {
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

            var validMessageIds = new List<long>();

            foreach (var message in messages)
            {
                if (await _userAuthService.IsConversationParticipant(message.ConversationId, userId))
                {
                    message.IsRead = true;
                    message.UpdatedAt = DateTime.UtcNow;
                    validMessageIds.Add(message.MessageId);
                }
            }

            await _context.SaveChangesAsync();

            return new MarkMessagesAsReadCommandResponse
            {
                MarkedMessageIds = validMessageIds.ToArray(),
                TotalMarkedCount = validMessageIds.Count
            };
        }
        catch (Exception)
        {
            return new MarkMessagesAsReadCommandResponse { ErrorMessage = "Failed to mark messages as read" };
        }
    }

    public async Task<MessageHistoryQueryResponse> GetMessageHistoryAsync(MessageHistoryQuery query, long userId)
    {
        try
        {
            // Validate conversation participant
            if (!await _userAuthService.IsConversationParticipant(query.ConversationId, userId))
            {
                return new MessageHistoryQueryResponse { ErrorMessage = "Access denied" };
            }

            var messagesQuery = _context.Messages
                .Where(m => m.ConversationId == query.ConversationId)
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

            var messageResponses = messages.Select(m => m.ToMessageResponse()).ToArray();
            var nextBefore = hasMore && messages.Count > 0 ? messages.Last().CreatedAt : (DateTime?)null;

            return new MessageHistoryQueryResponse
            {
                Messages = messageResponses,
                HasMore = hasMore,
                NextBefore = nextBefore
            };
        }
        catch (Exception)
        {
            return new MessageHistoryQueryResponse { ErrorMessage = "Failed to retrieve message history" };
        }
    }

    public async Task<ClientConversationListQueryResponse> GetClientConversationList(ConversationListQuery query, long clientId)
    {
        try
        {
            var clientPropertiesQuery = await _context.ClientsProperties.Where(i => i.ClientId == clientId)
                .Select(i => new
                {
                    i.AgentId,
                    AgentFirstName = i.Agent.User.FirstName,
                    AgentLastName = i.Agent.User.LastName,
                    i.Conversation,
                    LastMessage = i.Conversation.Messages.OrderByDescending(i => i.CreatedAt).FirstOrDefault()
                }).ToListAsync();

            var clientPropertiesGroupedByAgent = clientPropertiesQuery.GroupBy(i => i.AgentId);
            var groupedAgentConvosCount = clientPropertiesGroupedByAgent.Count();
            var conversations = new List<ClientConversationResponse>();

            foreach (var group in clientPropertiesGroupedByAgent.Skip(query.Offset).Take(query.Limit))
            {
                var latestConversationGroup = group.OrderByDescending(i => i.Conversation.UpdatedAt).FirstOrDefault();
                var unreadConvoCount = group.Where(i => i.Conversation.Messages?.Any(i => !i.IsRead ?? false) ?? false).Count();

                if (latestConversationGroup == null || latestConversationGroup.Conversation == null) continue;

                var conversation = new ClientConversationResponse()
                {
                    AgentName = latestConversationGroup.AgentFirstName + " " + latestConversationGroup.AgentLastName,
                    ConversationUpdatedAt = latestConversationGroup.Conversation.UpdatedAt,
                    ClickThroughConversationId = latestConversationGroup.Conversation.ConversationId,
                    LastMessage = latestConversationGroup.LastMessage?.ToMessageResponse(),
                    UnreadConversationCount = (byte)unreadConvoCount
                };

                conversations.Add(conversation);
            }

            return new ClientConversationListQueryResponse()
            {
                HasMore = query.Offset + conversations.Count < groupedAgentConvosCount,
                Conversations = [.. conversations.OrderByDescending(i => i.ConversationUpdatedAt) ],
                TotalCount = groupedAgentConvosCount
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new ClientConversationListQueryResponse { ErrorMessage = "Failed to retrieve conversations" };
        }
    }

    public async Task<AgentConversationListQueryResponse> GetAgentConversationListAsync(ConversationListQuery query, long agentId)
    {
        try
        {
            var convosList = await _context.Conversations
                .Where(i => i.ClientsProperties.Any(i => i.AgentId == agentId && i.DeletedAt == null))
                .Select(i => new
                {
                    ClientIds = i.ClientsProperties.Select(i => i.ClientId).OrderByDescending(i => i).ToList(),
                    ClientData = i.ClientsProperties.Select(x => new { x.ClientId, ClientName = x.Client.User.FirstName + " " + x.Client.User.LastName }).ToList(),
                    Conversation = i,
                    LastMessage = i.Messages.OrderByDescending(i => i.CreatedAt).FirstOrDefault()
                })
                .ToListAsync();

            var convosGrouped = convosList.GroupBy(i => string.Join(",", i.ClientIds.OrderByDescending(i => i)), x => x);

            var count = convosGrouped.Count();

            var convosGroupedByClients = new List<AgentConversationResponse>();

            foreach (var group in convosGrouped.Skip(query.Offset).Take(query.Limit))
            {
                var latestConversation = group.OrderByDescending(i => i.Conversation.UpdatedAt).FirstOrDefault();
                var unreadConvoCount = group.Where(i => i.Conversation.Messages?.Any(i => !i.IsRead ?? false) ?? false).Count();

                if (latestConversation == null || latestConversation.Conversation == null) continue;

                var conversation = new AgentConversationResponse()
                {
                    ConversationUpdatedAt = latestConversation.Conversation.UpdatedAt,
                    ClickThroughConversationId = latestConversation.Conversation.ConversationId,
                    Clients = group.FirstOrDefault()?.ClientData?
                        .Select(i => new ClientDetailsConversationResponse() { ClientId = i.ClientId, ClientName = i.ClientName })
                        .ToArray() ?? [],
                    LastMessage = latestConversation.LastMessage?.ToMessageResponse(),
                    UnreadConversationCount = (byte)unreadConvoCount
                };

                convosGroupedByClients.Add(conversation);
            }

            var hasMore = query.Offset + convosGroupedByClients.Count < count;

            return new AgentConversationListQueryResponse
            {
                Conversations = [.. convosGroupedByClients.OrderByDescending(i => i.ConversationUpdatedAt)],
                TotalCount = count,
                HasMore = hasMore
            };
        }
        catch (Exception)
        {
            return new AgentConversationListQueryResponse { ErrorMessage = "Failed to retrieve conversations" };
        }
    }
}