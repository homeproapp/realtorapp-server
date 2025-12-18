using RealtorApp.Contracts.Commands.Chat.Requests;
using RealtorApp.Contracts.Commands.Chat.Responses;
using RealtorApp.Contracts.Enums;
using RealtorApp.Contracts.Queries.Chat.Responses;
using RealtorApp.Infra.Data;

namespace RealtorApp.Domain.Extensions;

public static class ChatExtensions
{
    public static Message ToDbModel(this SendMessageCommand command)
    {
        return new Message
        {
            ConversationId = command.ConversationId,
            SenderId = command.SenderId,
            MessageText = command.MessageText,
        };
    }

    public static SendMessageCommandResponse ToSendMessageResponse(this Message message)
    {
        return new SendMessageCommandResponse
        {
            MessageId = message.MessageId,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            MessageText = message.MessageText,
            CreatedAt = message.CreatedAt,
            UpdatedAt = message.UpdatedAt,
            AttachmentResponses = message.Attachments?
                .Select(a => a.ToAttachmentResponse(_getAttachmentValues(a))).ToArray() ?? []
        };
    }

    public static MessageResponse ToMessageResponse(this Message message)
    {

        return new MessageResponse
        {
            MessageId = message.MessageId,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            MessageText = message.MessageText,
            CreatedAt = message.CreatedAt,
            UpdatedAt = message.UpdatedAt,
            AttachmentResponses = message.Attachments?
                .Select(a => a.ToAttachmentResponse(_getAttachmentValues(a))).ToArray() ?? []
        };
    }

    private static (AttachmentType Type, long ReferenceId) _getAttachmentValues(Attachment attachment)
    {
        var type = AttachmentType.Unknown;
        var referenceId = 0L;

        if (attachment.ContactAttachment != null)
        {
            type = AttachmentType.Contact;
            referenceId = attachment.ContactAttachment.ThirdPartyContactId;
        }
        else if (attachment.TaskAttachment != null)
        {
            type = AttachmentType.Task;
            referenceId = attachment.TaskAttachment.TaskId;
        }

        return (type, referenceId);
    }
}

public static class AttachmentExtensions
{
    public static AttachmentResponse ToAttachmentResponse(this Attachment attachment, (AttachmentType Type, long ReferenceId) attachmentValues)
    {

        return new AttachmentResponse
        {
            AttachmentId = attachment.AttachmentId,
            AttachmentType = attachmentValues.Type,
            MessageId = attachment.MessageId,
            ReferenceId = attachmentValues.ReferenceId
        };
    }
}
