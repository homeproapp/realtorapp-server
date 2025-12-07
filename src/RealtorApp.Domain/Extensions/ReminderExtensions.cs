using RealtorApp.Contracts.Commands.Reminders.Responses;
using RealtorApp.Contracts.Enums;
using RealtorApp.Infra.Data;

namespace RealtorApp.Domain.Extensions;

public static class ReminderExtensions
{
    public static AddOrUpdateReminderCommandResponse ToResponse(this Reminder reminder)
    {
        return new()
        {
          ReminderId = reminder.ReminderId,
          ReminderText = reminder.ReminderText,
          RemindAt = reminder.RemindAt,
          ReminderType = reminder.ReminderType.HasValue ? (ReminderType)reminder.ReminderType : ReminderType.Unknown,
          ReferencingObjectId = reminder.ReferencingObjectId
        };
    }
}
