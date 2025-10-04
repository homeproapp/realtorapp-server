using System;
using RealtorApp.Contracts.Enums;

namespace RealtorApp.Contracts.Queries.Tasks.Responses;

public class TaskFilesResponse
{
    public long TaskFileId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public FileTypes FileType { get; set; }

    public string FileUrl { get; set; } = string.Empty;
}
