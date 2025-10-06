using System;
using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Commands.Tasks.Responses;

public class AddOrUpdateTaskCommandResponse : ResponseWithError
{
    public long TaskId { get; set; }
    public AddedLinkResponse[]? AddedLinks { get; set; }
    public AddImageResponse[]? AddedImages { get; set; }
}

public class AddImageResponse
{
    public long FileId { get; set; }
}

public class AddedLinkResponse
{
    public long LinkId { get; set; }
    public required string LinkText { get; set; }
    public required string LinkUrl { get; set; } 
}