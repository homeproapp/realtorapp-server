using RealtorApp.Contracts.Enums;

namespace RealtorApp.Contracts.Commands.Reminders.Requests;

public class AddOrUpdateReminderCommand
{
  public long? ReminderId { get; set; }
  public required string ReminderText { get; set; }
  public ReminderType ReminderType { get; set; }
  public long ReferencingObjectId { get; set; }
  public DateTime RemindAt { get; set; }
  public long ListingId { get; set; }
}
