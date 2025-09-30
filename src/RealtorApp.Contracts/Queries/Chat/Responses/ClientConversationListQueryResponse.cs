using System;
using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Queries.Chat.Responses;

public class ClientConversationListQueryResponse : ResponseWithError
{
    public List<ClientConversationResponse> Conversations { get; set; } = [];
    public int TotalCount { get; set; }
    public bool HasMore { get; set; }
}

public class ClientConversationResponse
{
    public required string AgentName { get; set; }
    public required DateTime ConversationUpdatedAt { get; set; }
    public MessageResponse? LastMessage { get; set; }
    public long ClickThroughConversationId { get; set; }
    public byte UnreadConversationCount { get; set; }
}