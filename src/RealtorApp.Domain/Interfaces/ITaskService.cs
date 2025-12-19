using RealtorApp.Contracts.Commands.Tasks.Requests;
using RealtorApp.Contracts.Commands.Tasks.Responses;
using RealtorApp.Contracts.Common.Requests;
using RealtorApp.Contracts.Queries.Tasks.Requests;
using RealtorApp.Contracts.Queries.Tasks.Responses;

namespace RealtorApp.Domain.Interfaces;

public interface ITaskService
{
    Task<ListingTasksListDetailsQueryResponse> GetClientGroupedTasksListAsync(ListingsTaskListQuery query, long agentId);
    Task<Dictionary<long, TaskListItemResponse>> GetListingTasksAsync(ListingTasksQuery query, long listingId);
    Task<TaskListItemResponse?> GetTaskByIdAsync(long taskId, long listingId);
    Task<AddOrUpdateTaskCommandResponse> AddOrUpdateTaskAsync(AddOrUpdateTaskCommand command, long listingId, FileUploadRequest[] images);
    Task<bool> MarkTaskAndChildrenAsDeleted(long taskId, long listingId);
    Task<SlimListingTasksQueryResponse> GetSlimListingTasksAsync(long listingId);
    Task<int> UpdateTaskStatusAsync(long taskId, long listingId, Contracts.Enums.TaskStatus status);
}
