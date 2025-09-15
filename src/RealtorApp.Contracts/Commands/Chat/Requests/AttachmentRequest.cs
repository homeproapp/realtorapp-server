using RealtorApp.Contracts.Enums;

namespace RealtorApp.Contracts.Commands.Chat.Requests;

public class AttachmentRequest
{
    public long AttachmentObjectId { get; set; }
    public AttachmentType Type { get; set; }
}
