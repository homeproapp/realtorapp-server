using System;
using RealtorApp.Contracts.Commands.Tasks.Requests;
using RealtorApp.Contracts.Commands.Tasks.Responses;
using RealtorApp.Contracts.Queries.Tasks.Requests;
using RealtorApp.Contracts.Queries.Tasks.Responses;

namespace RealtorApp.Domain.Interfaces;

public interface ITaskService
{
    Task<ClientGroupedTasksListQueryResponse> GetClientGroupedTasksListAsync(ClientGroupedTasksListQuery query, long agentId);
    Task<ListingTasksQueryResponse> GetListingTasksAsync(ListingTasksQuery query, long listingId);
    Task<AddOrUpdateTaskCommandResponse> AddOrUpdateTaskAsync(AddOrUpdateTaskCommand command, long listingId);
    Task<bool> MarkTaskAndChildrenAsDeleted(long taskId);
    Task<SlimListingTasksQueryResponse> GetSlimListingTasksAsync(long listingId);
}
