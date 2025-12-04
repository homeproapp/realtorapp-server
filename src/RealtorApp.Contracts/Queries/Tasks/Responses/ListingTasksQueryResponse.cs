using System;
using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Queries.Tasks.Responses;

public class ListingTasksQueryResponse : ResponseWithError
{
    public TaskCompletionCountItem[] TaskCompletionCounts { get; set; } = [];
    public TaskListItemResponse[] Tasks { get; set; } = [];
    public Dictionary<string, List<TaskListFilterOptionsResponse>> FilterOptions { get; set; } = [];
}

public static class FilterOptions
{
    public const string ByRoom = "By room";
    public const string ByPriority = "By priority";
    public const string ByStatus = "By status";
}

public class TaskListFilterOptionsResponse
{
    public required string Label { get; set; }
}

public class TaskCompletionCountItem
{
    public required TaskCountType Type { get; set; }
    public required string Name { get; set; }
    public required double Completion { get; set; }
}

public enum TaskCountType
{
    Room,
    Priority,
    Total
}

public class TaskReminderSlim
{
    public long ReminderId { get; set; }
    public required string ReminderText { get; set; }
    public DateTime RemindAt { get; set; }
}

public class TaskListItemResponse
{
    public long TaskId { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; } = string.Empty;

    public List<TaskReminderSlim> TaskReminders { get; set; } = [];

    public required string Room { get; set; }

    public required short Priority { get; set; }

    public required short Status { get; set; }

    public DateTime? FollowUpDate { get; set; }

    public int? EstimatedCost { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual TaskFilesResponse[] TaskFiles { get; set; } = [];

    public virtual LinkResponse[] Links { get; set; } = [];

}
