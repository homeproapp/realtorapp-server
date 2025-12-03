using RealtorApp.Contracts.Common;
using RealtorApp.Contracts.Enums;

namespace RealtorApp.Contracts.Queries.User.Responses;

public class DashboardQueryResponse : ResponseWithError
{
    public DashboardGlanceItem[] DashboardGlanceItems { get; set; } = [];
    public UpcomingReminder[] UpcomingReminders { get; set; } = [];
}

public class DashboardGlanceItem
{
    public long Id { get; set; }
    public required string Title { get; set; }
    public int SortOrder { get; set; }
    public required string Type { get; set; }
    public int Value { get; set; }
}

public class UpcomingReminder
{
    public long ReminderId { get; set; }
    public required string ReminderText { get; set; }
    public DateTime DueDate { get; set; }
    public ReminderType ReminderType { get; set; }
    public long ReferencingObjectId { get; set; }
}
