using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RealtorApp.Contracts.Enums;
using RealtorApp.Domain.Extensions;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Models;
using File = RealtorApp.Domain.Models.File;

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

    public async Task<bool> UploadProfileImage(long userId, Stream fileData, string fileName)
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
            FileExtension = Path.GetExtension(fileName)
        };
        
        user.ProfileImageId = null;
        user.ProfileImage = file;

        var success = await UploadFile(file, fileData);

        if (!success)
        {
            return false;
        }

        await _context.SaveChangesAsync();
        return true;
    } 

    private async Task<bool> UploadFile(File file, Stream fileData)
    {
        var folderName = $"{file.FileType.Name.ToLowerInvariant()}s";
        var key = $"{folderName}/{file.Uuid}{file.FileExtension}";
        var contentType = file.GetContentType();

        return await _s3Service.UploadFileAsync(key, fileData, contentType);
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
