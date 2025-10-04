using Microsoft.EntityFrameworkCore;
using RealtorApp.Contracts.Queries;
using RealtorApp.Contracts.Queries.Tasks.Requests;
using RealtorApp.Contracts.Queries.Tasks.Responses;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Models;
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
}
