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

    public async Task<GetMessageHistoryQueryResponse> GetMessageHistoryAsync(GetMessageHistoryQuery query, long userId)
    {
        try
        {
            // Validate conversation participant
            if (!await _userAuthService.IsConversationParticipant(query.ConversationId, userId))
            {
                return new GetMessageHistoryQueryResponse { ErrorMessage = "Access denied" };
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

            return new GetMessageHistoryQueryResponse
            {
                Messages = messageResponses,
                HasMore = hasMore,
                NextBefore = nextBefore
            };
        }
        catch (Exception)
        {
            return new GetMessageHistoryQueryResponse { ErrorMessage = "Failed to retrieve message history" };
        }
    }

    // the conversation data presented to a client and an agent is different
    // we need separate functionality to handle these cases


    public async Task<GetConversationListQueryResponse> GetAgentConversationListAsync(GetConversationListQuery query, long agentId)
    {
        try
        {
            var limit = Math.Min(query.Limit, 50);
            var sql = _sqlQueryService.GetChatQuery("GetAgentConversationList");

            var results = await _context.Database
                .SqlQueryRaw<ConversationQueryResult>(sql, agentId, query.Offset, limit)
                .AsNoTracking()
                .ToListAsync();

            var conversations = results.Select(r => new ConversationResponse
            {
                ClickThroughConversationId = r.ClickThroughConversationId,
                AgentId = r.AgentId,
                Clients = ParseClientNamesData(r.ClientNamesData),
                LastMessage = r.MessageId.HasValue ? new MessageResponse
                {
                    MessageId = r.MessageId.Value,
                    ConversationId = r.ClickThroughConversationId,
                    SenderId = r.SenderId ?? 0,
                    MessageText = r.MessageText ?? "",
                    CreatedAt = r.CreatedAt ?? DateTime.UtcNow,
                    UpdatedAt = r.CreatedAt ?? DateTime.UtcNow,
                    AttachmentResponses = []
                } : null,
                UnreadConversationCount = r.UnreadConversationCount
            }).ToList();

            var totalCount = results.FirstOrDefault()?.TotalCount ?? 0;
            var hasMore = query.Offset + conversations.Count < totalCount;

            return new GetConversationListQueryResponse
            {
                Conversations = conversations,
                TotalCount = totalCount,
                HasMore = hasMore
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new GetConversationListQueryResponse { ErrorMessage = "Failed to retrieve conversations" };
        }
    }

    private static ClientConversationResponse[] ParseClientNamesData(string? clientNamesData)
    {
        if (string.IsNullOrEmpty(clientNamesData))
            return [];

        return clientNamesData
            .Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(entry =>
            {
                var parts = entry.Split(':', 2);
                return new ClientConversationResponse
                {
                    ClientId = long.Parse(parts[0]),
                    ClientName = parts.Length > 1 ? parts[1] : ""
                };
            })
            .ToArray();
    }
}