using RealtorApp.Contracts.Common;
using RealtorApp.Contracts.Commands.Chat.Responses;

namespace RealtorApp.Contracts.Queries.Chat.Responses;

public class MessageHistoryQueryResponse : ResponseWithError
{
    public MessageResponse[] Messages { get; set; } = [];
    public bool HasMore { get; set; }
    public DateTime? NextBefore { get; set; }
}

public class MessageResponse
{
    public long MessageId { get; set; }
    public long ConversationId { get; set; }
    public long SenderId { get; set; }
    public string MessageText { get; set; } = string.Empty;
    public bool? IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public AttachmentResponse[] AttachmentResponses { get; set; } = [];
}