namespace RealtorApp.Contracts.Queries.Chat.Requests;

public class MessageHistoryQuery
{
    public long ConversationId { get; set; }
    public int Limit { get; set; } = 50;
    public DateTime? Before { get; set; }
}