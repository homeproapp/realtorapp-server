using System;
using System.Text.Json.Serialization;

namespace RealtorApp.Contracts.Commands.Tasks.Requests;

public class AddOrUpdateTaskCommand
{
    public long? Id { get; set; }
    public long? TitleId { get; set; }
    public string? TitleString { get; set; }
    public required string Room { get; set; }
    public string? Description { get; set; }
    public LinkRequest[]? Links { get; set; }
    //TODO: Images as IFormFile in controller
}

public class LinkRequest
{
    public required string LinkText { get; set; }
    public required string LinkUrl { get; set; }
}