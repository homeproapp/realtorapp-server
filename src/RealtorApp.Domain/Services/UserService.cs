using Microsoft.EntityFrameworkCore;
using RealtorApp.Contracts.Enums;
using RealtorApp.Contracts.Queries.User.Responses;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Infra.Data;
using TaskStatus = RealtorApp.Contracts.Enums.TaskStatus;

namespace RealtorApp.Domain.Services;

public class UserService(RealtorAppDbContext dbContext) : IUserService
{
    private readonly RealtorAppDbContext _dbContext = dbContext;

    public async Task<DashboardQueryResponse> GetAgentDashboard(long userId)
    {
        var counts = await _dbContext.Agents
            .Where(i => i.UserId == userId)
            .Select(i => new
            {
                CompletedTasks = i.AgentsListings.SelectMany(x => x.Listing.Tasks
                    .Where(x => x.Status == (short)TaskStatus.Completed))
                    .Count(),
                TasksInProgress = i.AgentsListings.SelectMany(x => x.Listing.Tasks
                    .Where(x => x.Status == (short)TaskStatus.InProgress))
                    .Count(),
                ActiveClients = i.AgentsListings.Select(x => x.Listing.ClientsListings).Count(),
                ActiveListings = i.AgentsListings.Count,
                UpcomingReminders = i.User.Reminders
                    .Where(x => x.RemindAt > DateTime.UtcNow)
                    .OrderBy(x => x.RemindAt)
                    .Select(x => new UpcomingReminder()
                    {
                        ReminderId = x.ReminderId,
                        ReminderText = x.ReminderText,
                        DueDate = x.RemindAt,
                        ListingId = x.ListingId,
                        ReminderType = x.ReminderType == null ? ReminderType.Unknown : (ReminderType)x.ReminderType,
                        ReferencingObjectId = x.ReferencingObjectId
                    }).ToArray(),
            }).FirstOrDefaultAsync();
        if (counts == null)
        {
            return new();
        }

        return new()
        {
            DashboardGlanceItems =
            [
                new()
                {
                    Id = 1,
                    Title = "Active Clients",
                    Value = counts.ActiveClients,
                    SortOrder = 1,
                    Type = "Count"
                },
                new()
                {
                    Id = 2,
                    Title = "Active Listings",
                    Value = counts.ActiveListings,
                    SortOrder = 2,
                    Type = "Count"
                },
                new()
                {
                    Id = 3,
                    Title = "Tasks In Progress",
                    Value = counts.TasksInProgress,
                    SortOrder = 3,
                    Type = "Count"
                },
                new()
                {
                    Id = 4,
                    Title = "Tasks Completed",
                    Value = counts.CompletedTasks,
                    SortOrder = 4,
                    Type = "Count"
                },
            ],
            UpcomingReminders = counts.UpcomingReminders
        };
    }

    public async Task<DashboardQueryResponse> GetClientDashboard(long userId)
    {
        var counts = await _dbContext.Clients
            .Where(i => i.UserId == userId)
            .Select(i => new
            {
                CompletedTasks = i.ClientsListings.SelectMany(x => x.Listing.Tasks
                    .Where(x => x.Status == (short)TaskStatus.Completed))
                    .Count(),
                TasksInProgress = i.ClientsListings.SelectMany(x => x.Listing.Tasks
                    .Where(x => x.Status == (short)TaskStatus.InProgress))
                    .Count(),
                UpcomingReminders = i.User.Reminders
                    .Where(x => x.RemindAt > DateTime.UtcNow)
                    .OrderBy(x => x.RemindAt)
                    .Select(x => new UpcomingReminder()
                    {
                        ReminderId = x.ReminderId,
                        ReminderText = x.ReminderText,
                        DueDate = x.RemindAt,
                        ListingId = x.ListingId,
                        ReminderType = x.ReminderType == null ? ReminderType.Unknown : (ReminderType)x.ReminderType,
                        ReferencingObjectId = x.ReferencingObjectId
                    }).ToArray(),
            }).FirstOrDefaultAsync();
        if (counts == null)
        {
            return new();
        }

        return new()
        {
            DashboardGlanceItems =
            [
                new()
                {
                    Id = 1,
                    Title = "Tasks In Progress",
                    Value = counts.TasksInProgress,
                    SortOrder = 1,
                    Type = "Count"
                },
                new()
                {
                    Id = 2,
                    Title = "Tasks Completed",
                    Value = counts.CompletedTasks,
                    SortOrder = 2,
                    Type = "Count"
                },
            ],
            UpcomingReminders = counts.UpcomingReminders
        };
    }

    public async Task<User> GetOrCreateUserAsync(string firebaseUid, string email, string? displayName, bool isClient)
    {

      var existingUser = await _dbContext.Users
          .Where(u => u.Uuid == firebaseUid)
          .Select(u => new User
          {
              UserId = u.UserId,
              Uuid = u.Uuid,
              Agent = u.Agent != null ? new Agent { UserId = u.UserId } : null,
              Client = u.Client != null ? new Client { UserId = u.UserId } : null
          })
          .FirstOrDefaultAsync();


        if (existingUser != null)
            return existingUser;

        var user = new User
        {
            Uuid = firebaseUid,
            Email = email,
        };

        if (isClient)
        {
            user.Client = new();
        } else
        {
            user.Agent = new();
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            var nameParts = displayName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            user.FirstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
            user.LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;
        }

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return user;
    }

    public async Task<string?> GetAgentName(long agentId)
    {
        return await _dbContext.Agents.Where(i => i.UserId == agentId)
            .Select(i => i.User.FirstName + " " + i.User.LastName)
            .FirstOrDefaultAsync();
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<UserProfileQueryResponse?> GetUserProfileAsync(long userId)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new UserProfileQueryResponse
            {
                UserId = u.UserId,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Phone = u.Phone,
                ProfileImageId = u.ProfileImageId,
                ListingId = u.Client == null ? null :
                    u.Client.ClientsListings.Select(i => i.ListingId).FirstOrDefault(),
                Role = u.Agent != null ? "agent" : u.Client != null ? "client" : "unknown"
            })
            .FirstOrDefaultAsync();

        return user;
    }
}
