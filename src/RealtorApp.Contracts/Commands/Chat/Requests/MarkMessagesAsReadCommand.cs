namespace RealtorApp.Contracts.Commands.Chat.Requests;

public class MarkMessagesAsReadCommand
{
    public long[] MessageIds { get; set; } = [];
}