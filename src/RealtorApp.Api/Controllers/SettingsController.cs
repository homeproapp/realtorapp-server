using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RealtorApp.Contracts.Commands.Settings.Requests;
using RealtorApp.Contracts.Commands.Settings.Responses;
using RealtorApp.Contracts.Common.Requests;
using RealtorApp.Domain.Constants;
using RealtorApp.Domain.Interfaces;

namespace RealtorApp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting(RateLimitConstants.Authenticated)]
public class SettingsController(IUserSettingsService userSettingsService) : RealtorApiBaseController
{
    private readonly IUserSettingsService _userSettingsService = userSettingsService;

    [HttpPut("v1/profile")]
    public async Task<ActionResult<UpdateProfileCommandResponse>> UpdateProfile([FromBody] UpdateProfileCommand command)
    {
        var result = await _userSettingsService.UpdateProfileAsync(RequiredCurrentUserId, command);

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPut("v1/email")]
    public async Task<ActionResult<UpdateEmailCommandResponse>> UpdateEmail([FromBody] UpdateEmailCommand command)
    {
        var result = await _userSettingsService.UpdateEmailAsync(
            RequiredCurrentUserId,
            RequiredCurrentUserUuid,
            command);

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPut("v1/password")]
    public async Task<ActionResult<ChangePasswordCommandResponse>> ChangePassword([FromBody] ChangePasswordCommand command)
    {
        var result = await _userSettingsService.ChangePasswordAsync(
            RequiredCurrentUserUuid,
            command);

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("v1/avatar")]
    public async Task<ActionResult<UploadAvatarCommandResponse>> UploadAvatar(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new UploadAvatarCommandResponse { ErrorMessage = "No file provided [SETTINGS_E010]" });
        }

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return BadRequest(new UploadAvatarCommandResponse { ErrorMessage = "Invalid file type [SETTINGS_E011]" });
        }

        const long maxSize = 5 * 1024 * 1024;
        if (file.Length > maxSize)
        {
            return BadRequest(new UploadAvatarCommandResponse { ErrorMessage = "File too large [SETTINGS_E012]" });
        }

        using var fileData = new FileUploadRequest
        {
            Content = file.OpenReadStream(),
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileExtension = Path.GetExtension(file.FileName),
            ContentLength = file.Length
        };

        var result = await _userSettingsService.UploadAvatarAsync(RequiredCurrentUserId, fileData);

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
