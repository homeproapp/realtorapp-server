using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RealtorApp.Contracts.Common.Requests;
using RealtorApp.Contracts.Enums;
using RealtorApp.Domain.DTOs;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Infra.Data;
using RealtorApp.Domain.Settings;
using File = RealtorApp.Infra.Data.File;
using Task = System.Threading.Tasks.Task;
using DbTask = RealtorApp.Infra.Data.Task;

namespace RealtorApp.Domain.Services;

public class ImagesService(RealtorAppDbContext context, IS3Service s3Service, ILogger<ImagesService> logger, AppSettings appSettings) : IImagesService
{
    private readonly RealtorAppDbContext _context = context;
    private readonly IS3Service _s3Service = s3Service;
    private readonly ILogger<ImagesService> _logger = logger;
    private readonly AppSettings _appSettings = appSettings;

    public async Task<(Stream? ImageStream, string? ContentType)> GetImageByFileIdAsync(long fileId)
    {
        var file = await _context.Files
            .Include(f => f.FileType)
            .Where(f => f.FileId == fileId && f.DeletedAt == null)
            .FirstOrDefaultAsync();

        if (file == null)
        {
            _logger.LogWarning("File not found with ID {FileId}", fileId);
            return (null, null);
        }

        return await GetImage(file);
    }

    public async Task<(Stream? ImageStream, string? ContentType)> GetImageByFileIdAndListingIdAsync(long fileId, long listingId)
    {
        var file = await _context.FilesTasks
            .Include(i => i.Task)
            .Include(f => f.File)
                .ThenInclude(f => f.FileType)
            .Where(f => f.Task.ListingId == listingId && f.FileId == fileId && f.File.DeletedAt == null)
            .FirstOrDefaultAsync();

        if (file == null)
        {
            _logger.LogWarning("File not found with ID {FileId}", fileId);
            return (null, null);
        }

        return await GetImage(file.File);
    }


    public async Task<bool> UploadProfileImage(long userId, FileUploadRequest fileData)
    {
        var fileType = await _context.FileTypes.FirstOrDefaultAsync(i => i.Name == FileTypes.Avatar.ToString());
        var user = await _context.Users.FindAsync(userId);

        // should never hit this case
        if (fileType == null || user == null)
        {
            return false;
        }

        var file = new File()
        {
            Uuid = Guid.NewGuid(),
            FileTypeId = fileType.FileTypeId,
            FileType = fileType,
            FileExtension = fileData.FileExtension,
        };

        user.ProfileImageId = null;
        user.ProfileImage = file;

        var response = await UploadFile(file, fileData);

        if (!response.Successful)
        {
            return false;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(int SucceededCount, int FailedCount)> UploadNewTaskImages(FileUploadRequest[] images, DbTask dbTask)
    {
        try
        {
            var tasks = new List<Task<FileUploadResponseDto>>();

            foreach (var image in images)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                var task = _s3Service.UploadFileAsync(_appSettings.Aws.S3.ImagesBucketName, fileName, image, FileTypes.Image.ToString());
                tasks.Add(task);
            }

            var completedTasks = await Task.WhenAll(tasks);
            var successfulCompletedTasks = completedTasks.Where(i => i.Successful).ToList();
            var failedCount = completedTasks.Length - successfulCompletedTasks.Count;

            if (completedTasks != null && successfulCompletedTasks.Any())
            {
                await AddFileReferencesToDb([.. successfulCompletedTasks], dbTask);
            }

            return (successfulCompletedTasks.Count, failedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during s3 upload");
            return (0, 0);
        }
    }

    private async Task AddFileReferencesToDb(FileUploadResponseDto[] uploadedFiles, DbTask task)
    {
        var fileTasks = uploadedFiles.Select(i => new FilesTask()
        {
            Task = task,
            File = new()
            {
                FileExtension = i.OriginalRequest.FileExtension,
                Uuid = Guid.Parse(i.FileKey.Replace(Path.GetExtension(i.FileKey), "")),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                FileTypeId = 2 // images filetype TODO: remove hardcode
            }
        });

        await _context.FilesTasks.AddRangeAsync(fileTasks);
    }

    private async Task<FileUploadResponseDto> UploadFile(File file, FileUploadRequest fileData)
    {
        var folderName = $"{file.FileType.Name.ToLowerInvariant()}";
        var key = $"{folderName}/{file.Uuid}{file.FileExtension}";

        return await _s3Service.UploadFileAsync(_appSettings.Aws.S3.ImagesBucketName, key, fileData);
    }

    public async Task<(Stream? ImageStream, string? ContentType)> GetImageByUserIdAsync(long userId)
    {
        var user = await _context.Users
            .Where(i => i.UserId == userId && i.ProfileImageId != null)
            .Include(i => i.ProfileImage)
                .ThenInclude(i => i!.FileType)
            .FirstOrDefaultAsync();

        if (user == null || user.ProfileImage == null)
        {
            _logger.LogWarning("File not found with user id {userId}", userId);
            return (null, null);
        }

        return await GetImage(user.ProfileImage);
    }

    private async Task<(Stream? ImageStream, string? ContentType)> GetImage(File file)
    {
        var folderName = file.FileType.Name.ToLower();
        var key = $"{folderName}/{file.Uuid}{file.FileExtension}";

        return await _s3Service.GetFileAsync(_appSettings.Aws.S3.ImagesBucketName, key);
    }

}
