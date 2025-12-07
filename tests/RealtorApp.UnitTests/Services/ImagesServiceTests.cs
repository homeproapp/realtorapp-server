using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RealtorApp.Contracts.Common.Requests;
using RealtorApp.Contracts.Enums;
using RealtorApp.Domain.DTOs;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Services;

namespace RealtorApp.UnitTests.Services;

public class ImagesServiceTests : TestBase
{
    private readonly Mock<ILogger<ImagesService>> _mockLogger;
    private readonly Mock<IS3Service> _mockS3Service;
    private readonly ImagesService _imagesService;

    public ImagesServiceTests()
    {
        _mockLogger = new Mock<ILogger<ImagesService>>();
        _mockS3Service = new Mock<IS3Service>();

        _mockS3Service.Setup(x => x.UploadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<FileUploadRequest>(), It.IsAny<string>()))
            .ReturnsAsync((string fileKey, FileUploadRequest request, string folderName) => new FileUploadResponseDto()
            {
                FileKey = Guid.NewGuid().ToString(),
                OriginalRequest = request,
                Successful = true
            });

        _mockS3Service.Setup(x => x.GetFileAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((null, null));

        _imagesService = new ImagesService(DbContext, _mockS3Service.Object, _mockLogger.Object, MockAppsettings.Object);
    }

    [Fact]
    public async Task UploadProfileImage_WithNewUser_CreatesNewFileAndAssignsToUser()
    {
        var fileType = TestDataManager.CreateFileType(FileTypes.Avatar.ToString());
        var user = TestDataManager.CreateUser("test@example.com", "Test", "User");

        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        var fileUploadRequest = new FileUploadRequest
        {
            Content = stream,
            FileName = "avatar.jpg",
            ContentType = "image/jpeg",
            FileExtension = ".jpg",
            ContentLength = stream.Length
        };

        var result = await _imagesService.UploadProfileImage(user.UserId, fileUploadRequest);

        Assert.True(result);

        var updatedUser = await DbContext.Users
            .Include(u => u.ProfileImage)
            .FirstAsync(u => u.UserId == user.UserId);

        Assert.NotNull(updatedUser.ProfileImage);
        Assert.Equal(".jpg", user!.ProfileImage!.FileExtension);
        Assert.NotEqual(Guid.Empty, user.ProfileImage.Uuid);
        Assert.Equal(fileType.FileTypeId, user.ProfileImage.FileTypeId);
    }

    [Fact]
    public async Task UploadProfileImage_WithExistingProfileImage_ReplacesOldProfileImage()
    {
        var fileType = TestDataManager.CreateFileType(FileTypes.Avatar.ToString());
        var oldFile = TestDataManager.CreateFile(fileType.FileTypeId, ".png");
        var user = TestDataManager.CreateUser("test@example.com", "Test", "User");

        user.ProfileImage = oldFile;
        DbContext.SaveChanges();

        var oldFileId = oldFile.FileId;

        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        var fileUploadRequest = new FileUploadRequest
        {
            Content = stream,
            FileName = "new-avatar.jpg",
            ContentType = "image/jpeg",
            FileExtension = ".jpg",
            ContentLength = stream.Length
        };

        var result = await _imagesService.UploadProfileImage(user.UserId, fileUploadRequest);

        Assert.True(result);

        var updatedUser = await DbContext.Users
            .Include(u => u.ProfileImage)
            .FirstAsync(u => u.UserId == user.UserId);

        Assert.NotNull(updatedUser.ProfileImage);
        Assert.NotEqual(oldFileId, updatedUser.ProfileImage.FileId);
        Assert.Equal(".jpg", updatedUser.ProfileImage.FileExtension);

        var oldFileStillExists = await DbContext.Files.FindAsync(oldFileId);
        Assert.NotNull(oldFileStillExists);
    }

    [Fact]
    public async Task UploadProfileImage_WithNonExistentUser_ReturnsFalse()
    {
        TestDataManager.CreateFileType(FileTypes.Avatar.ToString());

        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        var fileUploadRequest = new FileUploadRequest
        {
            Content = stream,
            FileName = "avatar.jpg",
            ContentType = "image/jpeg",
            FileExtension = ".jpg",
            ContentLength = stream.Length
        };

        var result = await _imagesService.UploadProfileImage(99999, fileUploadRequest);

        Assert.False(result);
    }

    [Fact]
    public async Task UploadProfileImage_WithNonExistentFileType_ReturnsFalse()
    {
        var user = TestDataManager.CreateUser("test@example.com", "Test", "User");

        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        var fileUploadRequest = new FileUploadRequest
        {
            Content = stream,
            FileName = "avatar.jpg",
            ContentType = "image/jpeg",
            FileExtension = ".jpg",
            ContentLength = stream.Length
        };

        var result = await _imagesService.UploadProfileImage(user.UserId, fileUploadRequest);

        Assert.False(result);
    }

    [Fact]
    public async Task GetImageByFileId_WithNonExistentFile_ReturnsNull()
    {
        var (stream, contentType) = await _imagesService.GetImageByFileIdAsync(99999);

        Assert.Null(stream);
        Assert.Null(contentType);
    }

    [Fact]
    public async Task GetImageByUserId_WithUserWithoutProfileImage_ReturnsNull()
    {
        var user = TestDataManager.CreateUser("test@example.com", "Test", "User");

        var (stream, contentType) = await _imagesService.GetImageByUserIdAsync(user.UserId);

        Assert.Null(stream);
        Assert.Null(contentType);
    }

}
