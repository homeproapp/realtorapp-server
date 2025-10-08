namespace RealtorApp.Domain.Interfaces;

public interface IImagesService
{
    Task<(Stream? ImageStream, string? ContentType)> GetImageByFileIdAsync(long fileId);
Task<(Stream? ImageStream, string? ContentType)> GetImageByUserIdAsync(long userId);
}
