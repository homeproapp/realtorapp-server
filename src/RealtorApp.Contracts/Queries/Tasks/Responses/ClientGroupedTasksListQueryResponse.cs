using RealtorApp.Contracts.Common;
using RealtorApp.Contracts.Queries;
using TaskStatus = RealtorApp.Contracts.Enums.TaskStatus;
namespace RealtorApp.Contracts.Queries.Tasks.Responses;

public class ClientGroupedTasksListQueryResponse : ResponseWithError
{
    public List<ClientGroupedTasksDetailItem> ClientGroupedTasksDetails { get; set; } = [];
    public int TotalCount { get; set; }
    public bool HasMore { get; set; }
}

public class ClientGroupedTasksDetailItem
{
    public ClientListItemResponse[] Clients { get; set; } = [];
    public long ClickThroughListingId { get; set; }
    public Dictionary<TaskStatus, int> TaskStatusCounts { get; set; } = [];
    public byte TotalListings { get; set; }
}
