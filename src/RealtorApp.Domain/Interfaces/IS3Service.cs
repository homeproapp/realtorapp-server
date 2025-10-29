using RealtorApp.Contracts.Common.Requests;
using RealtorApp.Domain.DTOs;

namespace RealtorApp.Domain.Interfaces;

public interface IS3Service
{
    Task<FileUploadResponseDto> UploadFileAsync(string key, FileUploadRequest fileUploadRequest, string folderName = "");
    Task<(Stream? FileStream, string? ContentType)> GetFileAsync(string key);
}
