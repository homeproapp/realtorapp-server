using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Queries.Reminders.Responses;

public class ReminderTasksQueryResponse : ResponseWithError
{
    public ReminderTaskItem[] Tasks { get; set; } = [];
}

public class ReminderTaskItem
{
    public long TaskId { get; set; }
    public required string Title { get; set; }
}
