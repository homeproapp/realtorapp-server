using FirebaseAdmin.Auth;
using Microsoft.EntityFrameworkCore;
using RealtorApp.Contracts.Commands.Reminders.Requests;
using RealtorApp.Contracts.Commands.Reminders.Responses;
using RealtorApp.Contracts.Enums;
using RealtorApp.Contracts.Queries.Reminders.Responses;
using RealtorApp.Domain.Extensions;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Infra.Data;

namespace RealtorApp.Domain.Services;

public class ReminderService(RealtorAppDbContext dbContext) : IReminderService
{
    private readonly RealtorAppDbContext _dbContext = dbContext;

    public async Task<AddOrUpdateReminderCommandResponse> AddOrUpdateReminder(AddOrUpdateReminderCommand command, long userId)
    {
        Reminder result;
        if (command.ReminderId.HasValue)
        {
            result = await UpdateReminder(command, userId);
        } else
        {
            result = await AddReminder(command, userId);
        }

        await _dbContext.SaveChangesAsync();

        return result.ToResponse();
    }

    public async Task<ReminderDetailsQueryResponse> GetReminderDetails(long userId, long reminderId)
    {
        var reminder = await _dbContext.Reminders.FirstOrDefaultAsync(i => i.UserId == userId && i.ReminderId == reminderId);

        if (reminder == null)
        {
            return new() { ReminderText = string.Empty, ErrorMessage = "Error finding reminder" };
        }

        if (reminder.ReminderType == (short)ReminderType.Task)
        {
            var reminderTask = await _dbContext.Tasks.FirstOrDefaultAsync(i => i.TaskId == reminder.ReferencingObjectId);
            if (reminderTask != null)
            {
                return new()
                {
                    ReminderId = reminder.ReminderId,
                    ReminderText = reminder.ReminderText,
                    RemindeAt = reminder.RemindAt,
                    ReminderType = reminder.ReminderType.HasValue ? (ReminderType)reminder.ReminderType : ReminderType.Unknown,
                    ListingId = reminderTask.ListingId,
                    Task = reminderTask.ToSlimTask(),
                };
            }
        }

        return new()
        {
            ReminderId = reminder.ReminderId,
            ReminderText = reminder.ReminderText,
            RemindeAt = reminder.RemindAt,
            ReminderType = reminder.ReminderType.HasValue ? (ReminderType)reminder.ReminderType : ReminderType.Unknown,
            ListingId = reminder.ListingId
        };
    }

    public async Task<Reminder[]> GetUsersTaskReminders(long userId, long[] taskIds)
    {
        var reminders = await _dbContext.Reminders
            .Where(i => i.UserId == userId && taskIds.Contains(i.ReferencingObjectId) && i.DeletedAt == null &&
                (i.IsCompleted == null || i.IsCompleted == false) )
            .ToArrayAsync();

        return reminders ?? [];
    }

    private async Task<Reminder> AddReminder(AddOrUpdateReminderCommand command, long userId)
    {
        var reminder = new Reminder()
        {
            UserId = userId,
            ReminderType = (short)command.ReminderType,
            ReminderText = command.ReminderText,
            ListingId = command.ListingId,
            RemindAt = command.RemindAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ReferencingObjectId = command.ReferencingObjectId
        };

        await _dbContext.Reminders.AddAsync(reminder);

        return reminder;
    }

    private async Task<Reminder> UpdateReminder(AddOrUpdateReminderCommand command, long userId)
    {
        if (command.ReminderId == null)
        {
            throw new ArgumentException("Command.ReminderId was null");
        }

        await _dbContext.Reminders.Where(i => i.UserId == userId && i.ReminderId == command.ReminderId)
            .ExecuteUpdateAsync(setter => setter
                .SetProperty(i => i.UpdatedAt, DateTime.UtcNow)
                .SetProperty(i => i.ReminderType, (short)command.ReminderType)
                .SetProperty(i => i.RemindAt, command.RemindAt)
                .SetProperty(i => i.ReminderText, command.ReminderText)
                .SetProperty(i => i.ReferencingObjectId, command.ReferencingObjectId));


        return new Reminder()
        {
            ReminderId = (long)command.ReminderId,
            ReminderType = (short)command.ReminderType,
            ReminderText = command.ReminderText,
            RemindAt = command.RemindAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ReferencingObjectId = command.ReferencingObjectId
        };
    }
}
