using RealtorApp.Contracts.Common;
using RealtorApp.Contracts.Enums;
using RealtorApp.Contracts.Queries.Tasks.Responses;

namespace RealtorApp.Contracts.Queries.Reminders.Responses;

public class ReminderDetailsQueryResponse : ResponseWithError
{
    public long ReminderId { get; set; }
    public required string ReminderText { get; set; }
    public long ListingId { get; set; }
    public DateTime RemindeAt { get; set; }
    public ReminderType ReminderType { get; set; } = ReminderType.Unknown;
    public SlimTaskResponse? Task { get; set; }
}
