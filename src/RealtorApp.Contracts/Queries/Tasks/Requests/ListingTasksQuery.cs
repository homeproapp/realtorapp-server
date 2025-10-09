using System;

namespace RealtorApp.Contracts.Queries.Tasks.Requests;

public class ListingTasksQuery
{
    public string? Filter { get; set; }
    public string? Sort { get; set; }
}
