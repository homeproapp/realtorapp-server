namespace RealtorApp.Domain.DTOs;

public class ConversationQueryResult
{
    public long ClickThroughConversationId { get; set; }
    public long AgentId { get; set; }
    public long? MessageId { get; set; }
    public string? MessageText { get; set; }
    public long? SenderId { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int UnreadConversationCount { get; set; }
    public string? ClientNamesData { get; set; }
    public int TotalCount { get; set; }
}