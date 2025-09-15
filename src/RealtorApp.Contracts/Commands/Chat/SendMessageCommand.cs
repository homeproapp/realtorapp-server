namespace RealtorApp.Contracts.Commands.Chat;

public class SendMessageCommand
{
    public long? MessageId { get; set; }
    public long ConversationId { get; set; }
    public long SenderId { get; set; }
    public long PropertyId { get; set; }
    public string MessageText { get; set; } = string.Empty;
    public AttachmentRequest[] AttachmentRequests { get; set; } = [];
}
