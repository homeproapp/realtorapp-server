using RealtorApp.Contracts.Common;
using TaskStatus = RealtorApp.Contracts.Enums.TaskStatus;
namespace RealtorApp.Contracts.Queries.Tasks.Responses;

public class ListingTasksListDetailsQueryResponse : ResponseWithError
{
    public List<ListingTaskDetailItem> ListingTaskDetails { get; set; } = [];
    public int TotalCount { get; set; }
    public bool HasMore { get; set; }
}

public class ListingTaskDetailItem
{
    public ClientListItemResponse[] Clients { get; set; } = [];
    public long ListingId { get; set; }
    public required string Address { get; set; }
    public Dictionary<TaskStatus, int> TaskStatusCounts { get; set; } = [];
}
