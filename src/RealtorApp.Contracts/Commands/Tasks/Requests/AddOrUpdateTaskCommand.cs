using System;
using System.Text.Json.Serialization;
using RealtorApp.Contracts.Enums;

namespace RealtorApp.Contracts.Commands.Tasks.Requests;

public class AddOrUpdateTaskCommand
{
    public long? TaskId { get; set; } // null means new task, otherwise update existing
    public long? TitleId { get; set; }
    public required string TitleString { get; set; }
    public required string Room { get; set; }
    public string? Description { get; set; }
    public string[] NewImages { get; set; } = [];
    public UpdateImageRequest[] ImagesToRemove { get; set; } = [];
    public required TaskPriority Priority { get; set; }
    public AddOrUpdateLinkRequest[] Links { get; set; } = [];
}

public class AddOrUpdateLinkRequest
{
    public long? LinkId { get; set; }
    public required string LinkText { get; set; }
    public required string LinkUrl { get; set; }
    public bool IsMarkedForDeletion { get; set; }
}

public class UpdateImageRequest // adding images comes from formfile in controller
{
    public long? FileTaskId { get; set; }
    public bool IsMarkedForDeletion { get; set; }
}