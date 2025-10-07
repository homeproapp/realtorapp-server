using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Commands.Chat.Responses;

public class SendMessageCommandResponse : ResponseWithError
{
    public long MessageId { get; set; }
    public long ConversationId { get; set; }
    public long SenderId { get; set; }
    public string LocalId { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public AttachmentResponse[] AttachmentResponses { get; set; } = [];
}
