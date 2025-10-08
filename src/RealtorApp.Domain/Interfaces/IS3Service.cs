namespace RealtorApp.Domain.Interfaces;

public interface IS3Service
{
    Task<bool> UploadFileAsync(string key, Stream fileStream, string contentType);
    Task<(Stream? FileStream, string? ContentType)> GetFileAsync(string key);
}
