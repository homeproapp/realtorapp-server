using System;
using RealtorApp.Contracts.Common.Requests;

namespace RealtorApp.Domain.DTOs;

public class FileUploadResponseDto
{
    public required string FileKey { get; set; }
    public bool Successful { get; set; }
    public required FileUploadRequest OriginalRequest { get; set; }
}
