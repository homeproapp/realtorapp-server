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
            MessageReads = [new() { ReaderId = command.SenderId }]
        };
    }

    public static SendMessageCommandResponse ToSendMessageResponse(this Message message)
    {
        return new SendMessageCommandResponse
        {
            MessageId = message.MessageId,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            SenderName = message.Sender?.FirstName + " " + message.Sender?.LastName,
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
            SenderName = message.Sender?.FirstName + " " + message.Sender?.LastName,
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
            ReferenceId = attachmentValues.ReferenceId,
            Text = GetAttachmentText(attachment, attachmentValues.Type)
        };
    }

    private static Dictionary<AttachmentTextType, string> GetAttachmentText(Attachment attachment, AttachmentType type)
    {
        return new()
        {
            { AttachmentTextType.Header, type.ToString() },
            { AttachmentTextType.Content, GetAttachmentContent(attachment) },
            { AttachmentTextType.Footer, GetAttachmentFooter(attachment) }
        };
    }

    // client will saturate this data on init message send
    private static string GetAttachmentContent(Attachment attachment)
    {
        if (attachment.ContactAttachment != null)
        {
            return attachment.ContactAttachment.ThirdPartyContact?.Name ?? string.Empty;
        }

        if (attachment.TaskAttachment != null)
        {
            return attachment.TaskAttachment.Task?.Title ?? string.Empty;
        }

        return string.Empty;
    }

    private static string GetAttachmentFooter(Attachment attachment)
    {
        if (attachment.ContactAttachment != null)
        {
            return attachment.ContactAttachment.ThirdPartyContact?.Trade ?? string.Empty;
        }

        if (attachment.TaskAttachment != null)
        {
            return attachment.TaskAttachment.Task == null ? string.Empty : ((Contracts.Enums.TaskStatus)attachment.TaskAttachment.Task.Status).ToFormattedString();
        }

        return string.Empty;
    }
}
