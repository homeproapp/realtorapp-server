using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RealtorApp.Contracts.Commands.Settings.Requests;
using RealtorApp.Contracts.Commands.Settings.Responses;
using RealtorApp.Contracts.Common.Requests;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Infra.Data;

namespace RealtorApp.Domain.Services;

public class UserSettingsService(
    RealtorAppDbContext dbContext,
    IAuthProviderService authProviderService,
    IImagesService imagesService,
    ILogger<UserSettingsService> logger) : IUserSettingsService
{
    private readonly RealtorAppDbContext _dbContext = dbContext;
    private readonly IAuthProviderService _authProviderService = authProviderService;
    private readonly IImagesService _imagesService = imagesService;
    private readonly ILogger<UserSettingsService> _logger = logger;

    public async Task<UpdateProfileCommandResponse> UpdateProfileAsync(long userId, UpdateProfileCommand command)
    {
        try
        {
            var updated = await _dbContext.Users
                .Where(u => u.UserId == userId && u.DeletedAt == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(u => u.FirstName, command.FirstName)
                    .SetProperty(u => u.LastName, command.LastName)
                    .SetProperty(u => u.Phone, command.Phone)
                    .SetProperty(u => u.UpdatedAt, DateTime.UtcNow));

            if (updated == 0)
            {
                _logger.LogWarning("User not found for profile update: UserId={UserId}", userId);
                return new UpdateProfileCommandResponse { ErrorMessage = "Update failed [SETTINGS_E001]" };
            }

            return new UpdateProfileCommandResponse
            {
                FirstName = command.FirstName,
                LastName = command.LastName,
                Phone = command.Phone
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for UserId={UserId}", userId);
            return new UpdateProfileCommandResponse { ErrorMessage = "Update failed [SETTINGS_E002]" };
        }
    }

    public async Task<UpdateEmailCommandResponse> UpdateEmailAsync(long userId, string firebaseUid, UpdateEmailCommand command)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId && u.DeletedAt == null)
            .Select(u => new { u.Email })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            _logger.LogWarning("User not found for email update: UserId={UserId}", userId);
            return new UpdateEmailCommandResponse { ErrorMessage = "Update failed [SETTINGS_E003]" };
        }

        var signInResult = await _authProviderService.SignInWithEmailAndPasswordAsync(user.Email, command.CurrentPassword);
        if (signInResult == null)
        {
            _logger.LogWarning("Password verification failed for email update: UserId={UserId}", userId);
            return new UpdateEmailCommandResponse { ErrorMessage = "Update failed [SETTINGS_E004]" };
        }

        var firebaseUpdateResult = await _authProviderService.UpdateEmailAsync(firebaseUid, command.NewEmail);
        if (!firebaseUpdateResult)
        {
            _logger.LogWarning("Firebase email update failed: UserId={UserId}", userId);
            return new UpdateEmailCommandResponse { ErrorMessage = "Update failed [SETTINGS_E005]" };
        }

        try
        {
            var updated = await _dbContext.Users
                .Where(u => u.UserId == userId && u.DeletedAt == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(u => u.Email, command.NewEmail)
                    .SetProperty(u => u.UpdatedAt, DateTime.UtcNow));

            if (updated == 0)
            {
                _logger.LogError("DB email update failed after Firebase success, rolling back: UserId={UserId}", userId);
                await _authProviderService.UpdateEmailAsync(firebaseUid, user.Email);
                return new UpdateEmailCommandResponse { ErrorMessage = "Update failed [SETTINGS_E006]" };
            }

            return new UpdateEmailCommandResponse { Email = command.NewEmail };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DB exception during email update, rolling back Firebase: UserId={UserId}", userId);
            await _authProviderService.UpdateEmailAsync(firebaseUid, user.Email);
            return new UpdateEmailCommandResponse { ErrorMessage = "Update failed [SETTINGS_E007]" };
        }
    }

    public async Task<ChangePasswordCommandResponse> ChangePasswordAsync(string firebaseUid, ChangePasswordCommand command)
    {
        var result = await _authProviderService.ChangePasswordAsync(firebaseUid, command.CurrentPassword, command.NewPassword);

        if (!result)
        {
            _logger.LogWarning("Password change failed for FirebaseUid={FirebaseUid}", firebaseUid);
            return new ChangePasswordCommandResponse { ErrorMessage = "Update failed [SETTINGS_E008]", Success = false };
        }

        return new ChangePasswordCommandResponse { Success = true };
    }

    public async Task<UploadAvatarCommandResponse> UploadAvatarAsync(long userId, FileUploadRequest fileData)
    {
        var success = await _imagesService.UploadProfileImage(userId, fileData);

        if (!success)
        {
            _logger.LogWarning("Avatar upload failed for UserId={UserId}", userId);
            return new UploadAvatarCommandResponse { ErrorMessage = "Upload failed [SETTINGS_E009]" };
        }

        var profileImageId = await _dbContext.Users
            .Where(u => u.UserId == userId)
            .Select(u => u.ProfileImageId)
            .FirstOrDefaultAsync();

        return new UploadAvatarCommandResponse { ProfileImageId = profileImageId };
    }
}
