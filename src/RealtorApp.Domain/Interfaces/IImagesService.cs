using RealtorApp.Contracts.Common.Requests;
using DbTask = RealtorApp.Domain.Models.Task;

namespace RealtorApp.Domain.Interfaces;

public interface IImagesService
{
    Task<(Stream? ImageStream, string? ContentType)> GetImageByFileIdAsync(long fileId);
    Task<(Stream? ImageStream, string? ContentType)> GetImageByUserIdAsync(long userId);
    Task<(int SucceededCount, int FailedCount)> UploadNewTaskImages(FileUploadRequest[] images, DbTask dbTask);
    Task<(Stream? ImageStream, string? ContentType)> GetImageByFileIdAndListingIdAsync(long fileId, long listingId);
}
