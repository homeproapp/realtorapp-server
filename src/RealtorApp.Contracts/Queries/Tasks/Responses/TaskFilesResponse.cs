using System;
using RealtorApp.Contracts.Enums;

namespace RealtorApp.Contracts.Queries.Tasks.Responses;

public class TaskFilesResponse
{
    public long FileTaskId { get; set; }
    public long FileId { get; set; }
    public required string FileTypeName { get; set; }
}
