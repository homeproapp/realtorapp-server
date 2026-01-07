namespace RealtorApp.Contracts.Queries.Chat.Requests;

public class ConversationListQuery
{
    public int Limit { get; set; } = 100;
    public int Offset { get; set; } = 0;
}
