using RealtorApp.Contracts.Commands.Reminders.Requests;
using RealtorApp.Contracts.Commands.Reminders.Responses;
using RealtorApp.Contracts.Queries.Reminders.Responses;
using RealtorApp.Infra.Data;

namespace RealtorApp.Domain.Interfaces;

public interface IReminderService
{
    Task<AddOrUpdateReminderCommandResponse> AddOrUpdateReminder(AddOrUpdateReminderCommand command, long userId);
    Task<Reminder[]> GetUsersTaskReminders(long userId, long[] taskIds);
    Task<ReminderDetailsQueryResponse> GetReminderDetails(long userId, long reminderId);
}
