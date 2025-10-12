using System;
using RealtorApp.Contracts.Enums;

namespace RealtorApp.Contracts.Queries.Tasks.Requests;

public class ListingTasksQuery
{
    public string? Filter { get; set; }
    public string? Sort { get; set; }
}

public class ListingTaskFilterRequest
{
    public required TaskFilterOptionType Type { get; set; }
    public required string Value { get; set; }
}
