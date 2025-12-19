using RealtorApp.Contracts.Enums;

namespace RealtorApp.Contracts.Commands.Chat.Responses;

public class AttachmentResponse
{
    public long AttachmentId { get; set; }
    public long MessageId { get; set; }
    public long ReferenceId { get; set; }
    public AttachmentType AttachmentType { get; set; }
    public Dictionary<AttachmentTextType, string> Text { get; set; } = [];

}
