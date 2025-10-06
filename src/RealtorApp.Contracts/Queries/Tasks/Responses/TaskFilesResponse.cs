using System;
using RealtorApp.Contracts.Enums;

namespace RealtorApp.Contracts.Queries.Tasks.Responses;

public class TaskFilesResponse
{
    public long FileId { get; set; }
    public required string FileTypeName { get; set; }
}
