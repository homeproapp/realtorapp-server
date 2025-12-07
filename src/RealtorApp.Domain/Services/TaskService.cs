using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RealtorApp.Contracts.Commands.Tasks.Requests;
using RealtorApp.Contracts.Commands.Tasks.Responses;
using RealtorApp.Contracts.Common.Requests;
using RealtorApp.Contracts.Queries;
using RealtorApp.Contracts.Queries.Tasks.Requests;
using RealtorApp.Contracts.Queries.Tasks.Responses;
using RealtorApp.Domain.Extensions;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Infra.Data;
using DbTask = RealtorApp.Infra.Data.Task;
using Task = System.Threading.Tasks.Task;
using TaskStatus = RealtorApp.Contracts.Enums.TaskStatus;

namespace RealtorApp.Domain.Services;

public class TaskService(RealtorAppDbContext dbContext, IS3Service s3Service, ILogger<TaskService> logger, IImagesService imagesService) : ITaskService
{
    private readonly RealtorAppDbContext _dbContext = dbContext;
    private readonly IS3Service _s3Service = s3Service;
    private readonly ILogger<TaskService> _logger = logger;
    private readonly IImagesService _imagesService = imagesService;

    public async Task<ClientGroupedTasksListQueryResponse> GetClientGroupedTasksListAsync(ClientGroupedTasksListQuery query, long agentId)
    {
        var clientsList = await _dbContext.AgentsListings
            .Where(i => i.AgentId == agentId)
            .AsNoTracking()
            .Select(i => new
            {
                ClientIds = i.Listing.ClientsListings.Select(cl => cl.ClientId).ToList(),
                Clients = i.Listing.ClientsListings.Select(cl => cl.Client.User).ToList(),
                i.ListingId,
                TaskStatuses = i.Listing.Tasks.Select(t => t.Status).ToList(),
                MaxTaskUpdatedAt = i.Listing.Tasks.Any() ? i.Listing.Tasks.Max(t => t.UpdatedAt) : DateTime.MinValue
            })
            .ToListAsync();

        var allGroups = clientsList.GroupBy(i => string.Join("|", i.ClientIds.OrderByDescending(x => x))).ToList();
        var totalCount = allGroups.Count;

        var clientsGroupedTasks = allGroups.Skip(query.Offset).Take(query.Limit);
        var detailItems = new List<ClientGroupedTasksDetailItem>();

        foreach (var group in clientsGroupedTasks)
        {
            var latestListing = group.OrderByDescending(g => g.MaxTaskUpdatedAt).FirstOrDefault() ?? group.First();

            var hasTasks = group.Any(i => i.TaskStatuses.Count != 0);

            Dictionary<TaskStatus, int> statusCounts = [];

            if (hasTasks)
            {
                foreach (var status in group.SelectMany(i => i.TaskStatuses))
                {
                    var statusEnum = (TaskStatus)status;
                    if (statusCounts.TryGetValue(statusEnum, out int value))
                    {
                        statusCounts[statusEnum] = ++value;
                    }
                    else
                    {
                        statusCounts[statusEnum] = 1;
                    }
                }
            }

            var clientsGroupedTask = new ClientGroupedTasksDetailItem
            {
                ClickThroughListingId = latestListing.ListingId,
                Clients = [.. latestListing.Clients.Select(c => new ClientListItemResponse
                {
                    ClientId = c.UserId,
                    FirstName = c.FirstName ?? string.Empty,
                    LastName = c.LastName ?? string.Empty,
                    ProfileImageId = c.ProfileImageId
                })],
                TotalListings = (byte)group.Select(g => g.ListingId).Distinct().Count(),
                TaskStatusCounts = statusCounts
            };

            detailItems.Add(clientsGroupedTask);
        }

        return new ClientGroupedTasksListQueryResponse
        {
            ClientGroupedTasksDetails = detailItems,
            TotalCount = totalCount,
            HasMore = query.Offset + query.Limit < totalCount
        };
    }

    public async Task<Dictionary<long, TaskListItemResponse>> GetListingTasksAsync(ListingTasksQuery query, long listingId)
    {
        var tasks = await _dbContext.Tasks
            .Where(t => t.ListingId == listingId)
            .AsNoTracking()
            .Select(t => new TaskListItemResponse
            {
                TaskId = t.TaskId,
                Title = t.Title,
                Room = t.Room,
                Priority = t.Priority,
                Description = t.Description,
                Status = t.Status,
                FollowUpDate = t.FollowUpDate,
                EstimatedCost = t.EstimatedCost,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                TaskFiles = t.FilesTasks.Select(tf => new TaskFilesResponse
                {
                    FileId = tf.FileId,
                    FileTaskId = tf.FileTaskId,
                    FileTypeName = tf.File.FileType.Name,
                }).ToArray(),
                Links = t.Links.Select(l => new LinkResponse
                {
                    LinkId = l.LinkId,
                    Url = l.Url,
                    Name = l.Name,
                }).ToArray()
            })
            .ToDictionaryAsync(i => i.TaskId, i => i);

        return tasks;
    }

    public async Task<SlimListingTasksQueryResponse> GetSlimListingTasksAsync(long listingId)
    {
        var tasks = await _dbContext.Tasks
            .AsNoTracking()
            .Where(i => i.ListingId == listingId)
            .Select(i => new SlimTaskResponse()
            {
                TaskId = i.TaskId,
                Title = i.Title,
                Status = i.Status,
                Priority = i.Priority,
                Room = i.Room
            }).ToArrayAsync();

        return new()
        {
            Tasks = tasks,
        };
    }

    public async Task<AddOrUpdateTaskCommandResponse> AddOrUpdateTaskAsync(AddOrUpdateTaskCommand command, long listingId, FileUploadRequest[] images)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            DbTask? response;
            List<Link> addedLinks = [];

            if (command.TaskId.HasValue)
            {
                var (dbTaskUpdated, newLinks) = await UpdateExistingTaskAsync(command);
                response = dbTaskUpdated;
                addedLinks = newLinks;

            }
            else
            {
                response = await AddNewTaskAsync(command, listingId);
            }

            if (response == null)
            {
                return new() { ErrorMessage = "Unable to update or add task" };
            }


            var (FailedCount, SucceededCount) = await _imagesService.UploadNewTaskImages(images, response);

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return command.TaskId.HasValue ? response.FromNewTaskToTaskCommandResponse() : response.FromExistingTaskToTaskCommandResponse(addedLinks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "error during upsert task, rolling back");
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> MarkTaskAndChildrenAsDeleted(long taskId, long listingId)
    {
        var task = await _dbContext.Tasks
            .Include(i => i.FilesTasks)
                .ThenInclude(i => i.File)
            .Include(i => i.Links)
            .FirstOrDefaultAsync(i => i.TaskId == taskId && i.ListingId == listingId);

        if (task == null)
        {
            return false;
        }

        task.DeletedAt = DateTime.UtcNow;
        foreach (var fileTask in task.FilesTasks)
        {
            fileTask.DeletedAt = DateTime.UtcNow;
            fileTask.File.DeletedAt = DateTime.UtcNow;
        }

        foreach (var link in task.Links)
        {
            link.DeletedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        return true;
    }

    private async Task<(DbTask? task, List<Link> addedLinks) > UpdateExistingTaskAsync(AddOrUpdateTaskCommand command)
    {
        if (command.RemoveReminderRequests.Length > 0)
        {
            var ids = command.RemoveReminderRequests.Select(i => i.ReminderId);
            var reminders = await _dbContext.Reminders
                .Where(i => ids.Contains(i.ReminderId)).ToListAsync();

            if (reminders != null && reminders.Count > 0)
            {
                reminders.ForEach(i => i.DeletedAt = DateTime.UtcNow);
            }
        }

        var existingTask = await _dbContext.Tasks
            .Include(t => t.Links)
            .Include(i => i.FilesTasks)
                .ThenInclude(i => i.File)
            .FirstOrDefaultAsync(t => t.TaskId == command.TaskId);

        if (existingTask == null)
        {
            return (null, []);
            // return new() { ErrorMessage = "Unable to find data" };
        }

        existingTask.Title = command.TitleString;
        existingTask.Room = command.Room;
        existingTask.Description = command.Description;
        existingTask.Priority = (short)command.Priority;

        var addedLinks = new List<Link>();

        foreach (var linkCommand in command.Links)
        {
            if (linkCommand.IsMarkedForDeletion && linkCommand.LinkId.HasValue)
            {
                var linkToRemove = existingTask.Links.FirstOrDefault(l => l.LinkId == linkCommand.LinkId);
                if (linkToRemove != null)
                {
                    linkToRemove.DeletedAt = DateTime.UtcNow;
                }
            }
            else if (!linkCommand.LinkId.HasValue)
            {
                var newLink = new Link
                {
                    Name = linkCommand.LinkText,
                    Url = linkCommand.LinkUrl,
                    TaskId = existingTask.TaskId
                };
                existingTask.Links.Add(newLink);
                addedLinks.Add(newLink);
            }
        }

        if (command.ImagesToRemove.Length > 0)
        {
            var ids = command.ImagesToRemove.Select(i => i.FileTaskId).ToHashSet();
            var fileTasksToDelete = existingTask.FilesTasks.Where(i => ids.Contains(i.FileTaskId));

            foreach (var fileTaskToDelete in fileTasksToDelete)
            {
                fileTaskToDelete.DeletedAt = DateTime.UtcNow;
                fileTaskToDelete.File.DeletedAt = DateTime.UtcNow;
            }
        }

        return (existingTask, addedLinks);
    }

    public async Task<int> UpdateTaskStatusAsync(long taskId, long listingId, TaskStatus status)
    {
        int rowsUpdated = await _dbContext.Tasks
            .Where(i => i.TaskId == taskId && i.ListingId == listingId)
            .ExecuteUpdateAsync(setter =>
                setter.SetProperty(i => i.Status, (short)status));

        return rowsUpdated;
    }

    private async Task<DbTask> AddNewTaskAsync(AddOrUpdateTaskCommand command, long listingId)
    {
        var newTask = new DbTask
        {
            Title = command.TitleString,
            ListingId = listingId,
            Room = command.Room,
            Description = command.Description,
            Priority = (short)command.Priority,
            Status = (short)TaskStatus.NotStarted,
            Links = []
        };

        if (command.Links != null)
        {
            foreach (var link in command.Links)
            {
                var newLink = new Link
                {
                    Name = link.LinkText,
                    Url = link.LinkUrl,
                    TaskId = newTask.TaskId
                };

                newTask.Links.Add(newLink);
            }
        }

        await _dbContext.Tasks.AddAsync(newTask);
        return newTask;
    }
}
