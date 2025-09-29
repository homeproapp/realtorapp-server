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

public class ChatService(RealtorAppDbContext context, IMemoryCache cache, IUserAuthService userAuthService) : IChatService
{
    private readonly RealtorAppDbContext _context = context;
    private readonly IMemoryCache _cache = cache;
    private readonly IUserAuthService _userAuthService = userAuthService;

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

            var sql = @"
                WITH client_groups AS (
                    -- Group clients by property to create unique client sets per conversation
                    SELECT
                        STRING_AGG(cp.client_id::text, ',' ORDER BY cp.client_id) as client_group_key,
                        cp.conversation_id,
                        ARRAY_AGG(cp.client_id ORDER BY cp.client_id) as client_ids
                    FROM clients_properties cp
                    WHERE cp.agent_id = {0} AND cp.deleted_at IS NULL
                    GROUP BY cp.conversation_id
                ),
                client_set_groups AS (
                    -- Group conversations that have identical client sets
                    SELECT
                        client_group_key,
                        ARRAY_AGG(conversation_id ORDER BY (
                            SELECT c.updated_at FROM conversations c WHERE c.conversation_id = cg.conversation_id
                        ) DESC) as conversation_ids,
                        client_ids,
                        -- Most recent conversation for click-through
                        (ARRAY_AGG(conversation_id ORDER BY (
                            SELECT c.updated_at FROM conversations c WHERE c.conversation_id = cg.conversation_id
                        ) DESC))[1] as click_through_conversation_id
                    FROM client_groups cg
                    GROUP BY client_group_key, client_ids
                ),
                ranked_results AS (
                    SELECT
                        csg.click_through_conversation_id,
                        {0} as agent_id,
                        -- Get most recent message across all conversations in this group
                        (
                            SELECT m.message_id
                            FROM messages m
                            WHERE m.conversation_id = ANY(csg.conversation_ids)
                              AND m.deleted_at IS NULL
                            ORDER BY m.created_at DESC
                            LIMIT 1
                        ) as message_id,
                        (
                            SELECT m.message_text
                            FROM messages m
                            WHERE m.conversation_id = ANY(csg.conversation_ids)
                              AND m.deleted_at IS NULL
                            ORDER BY m.created_at DESC
                            LIMIT 1
                        ) as message_text,
                        (
                            SELECT m.sender_id
                            FROM messages m
                            WHERE m.conversation_id = ANY(csg.conversation_ids)
                              AND m.deleted_at IS NULL
                            ORDER BY m.created_at DESC
                            LIMIT 1
                        ) as message_sender_id,
                        (
                            SELECT m.created_at
                            FROM messages m
                            WHERE m.conversation_id = ANY(csg.conversation_ids)
                              AND m.deleted_at IS NULL
                            ORDER BY m.created_at DESC
                            LIMIT 1
                        ) as message_created_at,
                        -- Count conversations with unread messages
                        (
                            SELECT COUNT(DISTINCT conv_id)
                            FROM unnest(csg.conversation_ids) as conv_id
                            WHERE EXISTS (
                                SELECT 1 FROM messages m
                                WHERE m.conversation_id = conv_id
                                  AND m.deleted_at IS NULL
                                  AND m.is_read = false
                                  AND m.sender_id != {0}
                            )
                        ) as unread_conversation_count,
                        -- Get client names as JSON
                        (
                            SELECT STRING_AGG(
                                u.user_id || ':' || TRIM(COALESCE(u.first_name, '') || ' ' || COALESCE(u.last_name, '')),
                                '|'
                                ORDER BY u.user_id
                            )
                            FROM unnest(csg.client_ids) as client_id
                            JOIN users u ON u.user_id = client_id
                        ) as client_names_data,
                        ROW_NUMBER() OVER (
                            ORDER BY (
                                SELECT m.created_at
                                FROM messages m
                                WHERE m.conversation_id = ANY(csg.conversation_ids)
                                  AND m.deleted_at IS NULL
                                ORDER BY m.created_at DESC
                                LIMIT 1
                            ) DESC NULLS LAST
                        ) as row_num
                    FROM client_set_groups csg
                ),
                paginated_results AS (
                    SELECT *
                    FROM ranked_results
                    WHERE row_num > {1} AND row_num <= {1} + {2}
                ),
                total_count AS (
                    SELECT COUNT(*) as total_count FROM ranked_results
                )
                SELECT
                    pr.click_through_conversation_id as ClickThroughConversationId,
                    pr.agent_id as AgentId,
                    pr.message_id as MessageId,
                    pr.message_text as MessageText,
                    pr.message_sender_id as SenderId,
                    pr.message_created_at as CreatedAt,
                    pr.unread_conversation_count as UnreadConversationCount,
                    pr.client_names_data as ClientNamesData,
                    tc.total_count as TotalCount
                FROM paginated_results pr
                CROSS JOIN total_count tc
                ORDER BY pr.row_num";

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