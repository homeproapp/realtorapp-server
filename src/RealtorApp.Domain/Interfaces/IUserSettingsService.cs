using RealtorApp.Contracts.Commands.Settings.Requests;
using RealtorApp.Contracts.Commands.Settings.Responses;
using RealtorApp.Contracts.Common.Requests;

namespace RealtorApp.Domain.Interfaces;

public interface IUserSettingsService
{
    Task<UpdateProfileCommandResponse> UpdateProfileAsync(long userId, UpdateProfileCommand command);
    Task<UpdateEmailCommandResponse> UpdateEmailAsync(long userId, string firebaseUid, UpdateEmailCommand command);
    Task<ChangePasswordCommandResponse> ChangePasswordAsync(string firebaseUid, ChangePasswordCommand command);
    Task<UploadAvatarCommandResponse> UploadAvatarAsync(long userId, FileUploadRequest fileData);
}
