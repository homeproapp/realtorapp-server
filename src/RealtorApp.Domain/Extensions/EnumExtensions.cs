using TaskStatus = RealtorApp.Contracts.Enums.TaskStatus;

namespace RealtorApp.Domain.Extensions;

public static class EnumExtensions
{
    public static string ToFormattedString(this TaskStatus status)
    {
        return status switch
        {
            TaskStatus.Cancelled => "Cancelled",
            TaskStatus.NotStarted => "Not Started",
            TaskStatus.Completed => "Completed",
            TaskStatus.InProgress => "In Progress",
            TaskStatus.OnHold => "On Hold",
            _ => "Unknown"
        };
    }
}
