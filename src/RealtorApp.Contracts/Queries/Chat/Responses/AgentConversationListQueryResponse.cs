using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Queries.Chat.Responses;

public class AgentConversationListQueryResponse : ResponseWithError
{
    public List<AgentConversationResponse> Conversations { get; set; } = [];
    public int TotalCount { get; set; }
    public bool HasMore { get; set; }
}

public class AgentConversationResponse
{
    public long ClickThroughConversationId { get; set; }
    public required DateTime ConversationUpdatedAt { get; set; }
    public ClientDetailsConversationResponse[] Clients { get; set; } = [];
    public MessageResponse? LastMessage { get; set; }
    public byte UnreadConversationCount { get; set; }
}

public class ClientDetailsConversationResponse
{
    public long ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
}