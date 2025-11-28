using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RealtorApp.Contracts.Commands.Tasks.Responses;
using RealtorApp.Contracts.Common.Requests;
using RealtorApp.Contracts.Enums;
using RealtorApp.Domain.DTOs;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Models;
using File = RealtorApp.Domain.Models.File;
using Task = System.Threading.Tasks.Task;

namespace RealtorApp.Domain.Services;

public class ImagesService(RealtorAppDbContext context, IS3Service s3Service, ILogger<ImagesService> logger) : IImagesService
{
    private readonly RealtorAppDbContext _context = context;
    private readonly IS3Service _s3Service = s3Service;
    private readonly ILogger<ImagesService> _logger = logger;

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
    
    public async Task UploadNewTaskImages(FileUploadRequest[] images, AddOrUpdateTaskCommandResponse response)
    {
        try
        {
            var tasks = new List<Task<FileUploadResponseDto>>();
            
            foreach (var image in images)
            {
                var task = _s3Service.UploadFileAsync(Guid.NewGuid().ToString(), image, FileTypes.Image.ToString());
                tasks.Add(task);
            }

            var completedTasks = await Task.WhenAll(tasks);
            var successfulCompletedTasks = completedTasks.Where(i => i.Successful).ToList();
            var failedCount = completedTasks.Length - successfulCompletedTasks.Count;
            
            if (failedCount > 0)
            {
                response.ErrorMessage = $"{failedCount} of {completedTasks.Length} images failed to upload";
            } 

            if (completedTasks != null && successfulCompletedTasks.Any())
            {
                await AddFileReferencesToDb([.. successfulCompletedTasks], response.TaskId);
            }
        }
        catch (Exception ex)
        {
            response.ErrorMessage = "Something went wrong uploading images";
            _logger.LogError("Error during s3 upload {ex}", ex);
        }
    }

    private async Task AddFileReferencesToDb(FileUploadResponseDto[] uploadedFiles, long taskId)
    {
        var fileTasks = uploadedFiles.Select(i => new FilesTask()
        {
            TaskId = taskId,
            File = new()
            {
                FileExtension = i.OriginalRequest.FileExtension,
                Uuid = Guid.Parse(i.FileKey),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        });

        await _context.FilesTasks.AddRangeAsync(fileTasks);
        await _context.SaveChangesAsync();
    }

    private async Task<FileUploadResponseDto> UploadFile(File file, FileUploadRequest fileData)
    {
        var folderName = $"{file.FileType.Name.ToLowerInvariant()}s";
        var key = $"{folderName}/{file.Uuid}{file.FileExtension}";

        return await _s3Service.UploadFileAsync(key, fileData);
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
        var folderName = $"{file.FileType.Name.ToLowerInvariant()}s";
        var key = $"{folderName}/{file.Uuid}{file.FileExtension}";

        return await _s3Service.GetFileAsync(key);
    }

}
