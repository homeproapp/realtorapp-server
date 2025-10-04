using System;
using RealtorApp.Contracts.Queries.Tasks.Requests;
using RealtorApp.Contracts.Queries.Tasks.Responses;

namespace RealtorApp.Domain.Interfaces;

public interface ITaskService
{
    Task<List<ClientGroupedTasksListQueryResponse>> GetClientGroupedTasksListAsync(ClientGroupedTasksListQuery query, long agentId);
}
