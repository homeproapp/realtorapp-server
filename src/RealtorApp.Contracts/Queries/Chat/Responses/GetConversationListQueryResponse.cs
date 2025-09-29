using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Queries.Chat.Responses;

public class GetConversationListQueryResponse : ResponseWithError
{
    public List<ConversationResponse> Conversations { get; set; } = [];
    public int TotalCount { get; set; }
    public bool HasMore { get; set; }
}

public class ConversationResponse
{
    public long ClickThroughConversationId { get; set; }
    public long AgentId { get; set; }
    public ClientConversationResponse[] Clients { get; set; } = []; // All clients in this grouped conversation
    public MessageResponse? LastMessage { get; set; } // Most recent message across all properties
    public int UnreadConversationCount { get; set; } // Number of property conversations with unread messages
}

public class ClientConversationResponse
{
    public long ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
}