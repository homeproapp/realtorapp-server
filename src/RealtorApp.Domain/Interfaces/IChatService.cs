using RealtorApp.Contracts.Commands.Chat.Requests;
using RealtorApp.Contracts.Commands.Chat.Responses;
using RealtorApp.Contracts.Queries.Chat.Requests;
using RealtorApp.Contracts.Queries.Chat.Responses;

namespace RealtorApp.Domain.Interfaces;

public interface IChatService
{
    Task<SendMessageCommandResponse> SendMessageAsync(SendMessageCommand command, HashSet<long> activeUserIds);
    Task<MarkMessagesAsReadCommandResponse> MarkMessagesAsReadAsync(MarkMessagesAsReadCommand command, long userId);
    Task<MessageHistoryQueryResponse> GetMessageHistoryAsync(MessageHistoryQuery query, long userId, long conversationId);
    Task<ConversationListQueryResponse> GetAgentConversationListAsync(ConversationListQuery query, long agentId);
    Task<ConversationListQueryResponse> GetClientConversationList(ConversationListQuery query, long clientId);
}
