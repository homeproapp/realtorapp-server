namespace RealtorApp.Contracts.Queries.Chat.Requests;

public class GetConversationListQuery
{
    public int Limit { get; set; } = 20;
    public int Offset { get; set; } = 0;
}