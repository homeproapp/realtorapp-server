using Microsoft.EntityFrameworkCore;
using RealtorApp.Contracts.Commands.Tasks.Requests;
using RealtorApp.Contracts.Commands.Tasks.Responses;
using RealtorApp.Contracts.Queries;
using RealtorApp.Contracts.Queries.Tasks.Requests;
using RealtorApp.Contracts.Queries.Tasks.Responses;
using RealtorApp.Domain.Extensions;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Models;
using Task = RealtorApp.Domain.Models.Task;
using TaskStatus = RealtorApp.Contracts.Enums.TaskStatus;

namespace RealtorApp.Domain.Services;

public class TaskService(RealtorAppDbContext dbContext) : ITaskService
{
    private readonly RealtorAppDbContext _dbContext = dbContext;

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
                    if (status == null) continue;
                    var statusEnum = (TaskStatus)status;
                    if (statusCounts.ContainsKey(statusEnum))
                    {
                        statusCounts[statusEnum]++;
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

    public async Task<ListingTasksQueryResponse[]> GetListingTasksAsync(ListingTasksQuery query, long listingId)
    {
        var tasks = await _dbContext.Tasks
            .Where(t => t.ListingId == listingId)
            .AsNoTracking()
            .Select(t => new ListingTasksQueryResponse
            {
                TaskId = t.TaskId,
                Title = t.Title,
                Room = t.Room,
                Priority = t.Priority,
                Status = t.Status,
                FollowUpDate = t.FollowUpDate,
                EstimatedCost = t.EstimatedCost,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                TaskFiles = t.FilesTasks.Select(tf => new TaskFilesResponse
                {
                    FileId = tf.FileId,
                    FileTypeName = tf.File.FileType.Name,
                }).ToArray(),
                Links = t.Links.Select(l => new LinkResponse
                {
                    LinkId = l.LinkId,
                    Url = l.Url,
                    Name = l.Name,
                }).ToArray()
            })
            .ToArrayAsync();

        return tasks ?? [];
    }

    public async Task<AddOrUpdateTaskCommandResponse> AddOrUpdateTaskAsync(AddOrUpdateTaskCommand command, long listingId)
    {
        if (command.TaskId.HasValue)
        {
            return await _updateExistingTaskAsync(command);
        }
        else
        {
            return await _addNewTaskAsync(command, listingId);
        }
    }

    public async Task<bool> MarkTaskAndChildrenAsDeleted(long taskId)
    {
        var task = await _dbContext.Tasks
            .Include(i => i.FilesTasks)
                .ThenInclude(i => i.File)
            .Include(i => i.Links)
            .FirstOrDefaultAsync(i => i.TaskId == taskId);

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

    private async Task<AddOrUpdateTaskCommandResponse> _updateExistingTaskAsync(AddOrUpdateTaskCommand command)
    {
        var existingTask = await _dbContext.Tasks
            .Include(t => t.Links)
            .FirstOrDefaultAsync(t => t.TaskId == command.TaskId);

        if (existingTask == null)
        {
            return new() { ErrorMessage = "Unable to find data" };
        }

        existingTask.Title = command.TitleString;
        existingTask.Room = command.Room;
        existingTask.Description = command.Description;
        existingTask.Priority = (short)command.Priority;

        var addedLinks = new List<Link>();

        foreach (var linkCommand in command.Links)
        {
            if (linkCommand.IsMarkedForDeletion)
            {
                var linkToRemove = existingTask.Links.FirstOrDefault(l => l.LinkId == linkCommand.LinkId);
                if (linkToRemove != null)
                {
                    linkToRemove.DeletedAt = DateTime.UtcNow;
                }
            }
            else
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

        await _dbContext.SaveChangesAsync();

        return existingTask.FromExistingTaskToTaskCommandResponse(addedLinks);
    }
    private async Task<AddOrUpdateTaskCommandResponse> _addNewTaskAsync(AddOrUpdateTaskCommand command, long listingId)
    {
        var newTask = new Task
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
        await _dbContext.SaveChangesAsync();

        return newTask.FromNewTaskToTaskCommandResponse();
    }
}
