using RealtorApp.Contracts.Enums;

namespace RealtorApp.Contracts.Commands.Chat;

public class AttachmentRequest
{
    public long AttachmentObjectId { get; set; }
    public AttachmentType Type { get; set; }
}
