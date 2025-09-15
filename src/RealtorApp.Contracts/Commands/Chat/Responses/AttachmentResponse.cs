using RealtorApp.Contracts.Enums;

namespace RealtorApp.Contracts.Commands.Chat.Responses;

public class AttachmentResponse
{
    public long AttachmentId { get; set; }
    public AttachmentType AttachmentType { get; set; }
    public Dictionary<string, AttachmentTextType> Text { get; set; } = [];

}
