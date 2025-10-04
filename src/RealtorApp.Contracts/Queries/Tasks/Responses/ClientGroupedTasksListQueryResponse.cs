using TaskStatus = RealtorApp.Contracts.Enums.TaskStatus;
namespace RealtorApp.Contracts.Queries.Tasks.Responses;

public class ClientGroupedTasksListQueryResponse
{
    public ClientListItemResponse[] Clients { get; set; } = [];
    public long ClickThroughListingId { get; set; }
    public Dictionary<TaskStatus, int> TaskStatusCounts { get; set; } = [];
    public byte TotalListings { get; set; }
}
