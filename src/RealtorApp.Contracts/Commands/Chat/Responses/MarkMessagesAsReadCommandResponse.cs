using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Commands.Chat.Responses;

public class MarkMessagesAsReadCommandResponse : ResponseWithError
{
    public long[] MarkedMessageIds { get; set; } = [];
    public int TotalMarkedCount { get; set; }
}