using RealtorApp.Contracts.Commands.Chat.Requests;
using RealtorApp.Contracts.Commands.Chat.Responses;
using RealtorApp.Contracts.Queries.Chat.Requests;
using RealtorApp.Contracts.Queries.Chat.Responses;

namespace RealtorApp.Domain.Interfaces;

public interface IChatService
{
    Task<SendMessageCommandResponse> SendMessageAsync(SendMessageCommand command);
    Task<MarkMessagesAsReadCommandResponse> MarkMessagesAsReadAsync(MarkMessagesAsReadCommand command, long userId);
    Task<GetMessageHistoryQueryResponse> GetMessageHistoryAsync(GetMessageHistoryQuery query, long userId);
    Task<GetConversationListQueryResponse> GetAgentConversationListAsync(GetConversationListQuery query, long userId);
}