using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Queries.Chat.Responses;

public class ConversationListQueryResponse : ResponseWithError
{
    public List<ConversationResponse> Conversations { get; set; } = [];
    public int TotalCount { get; set; }
    public bool HasMore { get; set; }
}

public class ConversationResponse
{
    public long ConversationId { get; set; }
    public required DateTime ConversationUpdatedAt { get; set; }
    public UserDetailsConversationResponse[] OtherUsers { get; set; } = [];
    public MessageResponse? LastMessage { get; set; }
    public required string Address { get; set; }
    public bool HasUnreadMessage { get; set; }
}

public class UserDetailsConversationResponse
{
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public long? ProfileImageId { get; set; }
}
