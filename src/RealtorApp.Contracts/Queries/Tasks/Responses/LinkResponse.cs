using System;

namespace RealtorApp.Contracts.Queries.Tasks.Responses;

public class LinkResponse
{
    public long LinkId { get; set; }

    public string? Url { get; set; }

    public string? Name { get; set; }
}
