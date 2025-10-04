using System;

namespace RealtorApp.Contracts.Queries.Tasks.Responses;

public class ListingTasksQueryResponse
{
    public long TaskId { get; set; }
    
    public string? Title { get; set; }

    public string? Room { get; set; }

    public short? Priority { get; set; }

    public short? Status { get; set; }

    public DateTime? FollowUpDate { get; set; }

    public int? EstimatedCost { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual TaskFilesResponse[] TaskFiles { get; set; } = [];

    public virtual LinkResponse[] Links { get; set; } = [];

}
