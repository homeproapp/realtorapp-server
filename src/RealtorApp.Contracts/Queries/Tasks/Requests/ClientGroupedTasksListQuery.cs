using System;

namespace RealtorApp.Contracts.Queries.Tasks.Requests;

public class ClientGroupedTasksListQuery
{
    public int Limit { get; set; } = 20;
    public int Offset { get; set; } = 0;
}
