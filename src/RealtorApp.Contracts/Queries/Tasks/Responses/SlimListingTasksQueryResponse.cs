using System;
using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Queries.Tasks.Responses;

public class SlimListingTasksQueryResponse : ResponseWithError
{
    public SlimTaskResponse[] Tasks { get; set; } = [];
}

public class SlimTaskResponse
{
    public long TaskId { get; set; }
    public required string Title { get; set; }
    public required string Room { get; set; }
    public required short Priority { get; set; }
    public required short Status { get; set; }
}
