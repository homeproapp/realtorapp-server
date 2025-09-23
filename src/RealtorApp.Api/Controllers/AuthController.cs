using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealtorApp.Contracts.Commands.Auth;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Settings;

namespace RealtorApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthController(
    IJwtService jwtService,
    IRefreshTokenService refreshTokenService,
    IUserAuthService userAuthService,
    IAuthProviderService authProviderService,
    AppSettings appSettings) : BaseController
{
    private readonly IJwtService _jwtService = jwtService;
    private readonly IRefreshTokenService _refreshTokenService = refreshTokenService;
    private readonly IUserAuthService _userAuthService = userAuthService;
    private readonly IAuthProviderService _authProviderService = authProviderService;
    private readonly AppSettings _appSettings = appSettings;

    [AllowAnonymous]
    [HttpPost("v1/login")]
    public async Task<ActionResult<LoginCommandResponse>> LoginAsync([FromBody] LoginCommand command)
    {
        // Validate Firebase token
        var firebaseUser = await _authProviderService.ValidateTokenAsync(command.FirebaseToken);
        if (firebaseUser == null)
        {
            return Unauthorized(new { error = "Authentication failed", code = "AUTH_E009" });
        }

        // TODO: Find existing user by Firebase UID or create new agent user
        // For now, use placeholder values
        var userUuid = Guid.Parse(firebaseUser.Uid); // Use Firebase UID as user UUID
        var role = "agent"; // This should be determined from user lookup/creation
        var userId = 1L; // This should come from database lookup/creation

        var accessToken = _jwtService.GenerateAccessToken(userUuid, role);
        var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(userId);

        return Ok(new LoginCommandResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _appSettings.Jwt.AccessTokenExpirationMinutes * 60
        });
    }

    [HttpPost("v1/refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<RefreshTokenCommandResponse>> RefreshTokenAsync([FromBody] RefreshTokenCommand command)
    {
        // Get user by refresh token (validates token and gets user data in one call)
        var userDto = await _refreshTokenService.GetUserByRefreshTokenAsync(command.RefreshToken);
        if (userDto == null)
        {
            return Unauthorized(new { error = "Authentication failed", code = "AUTH_E004" });
        }

        // Generate new access token using data from DTO
        var accessToken = _jwtService.GenerateAccessToken(userDto.UserUuid, userDto.Role);

        // Generate new refresh token (token rotation)
        var newRefreshToken = await _refreshTokenService.CreateRefreshTokenAsync(userDto.UserId);

        // Revoke old refresh token
        await _refreshTokenService.RevokeRefreshTokenAsync(command.RefreshToken);

        return Ok(new RefreshTokenCommandResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = _appSettings.Jwt.AccessTokenExpirationMinutes * 60
        });
    }

    [HttpPost("v1/logout")]
    public async Task<ActionResult> LogoutAsync([FromBody] LogoutCommand command)
    {
        // Revoke the specific refresh token
        await _refreshTokenService.RevokeRefreshTokenAsync(command.RefreshToken);

        return Ok(new { message = "Logout successful" });
    }

    [HttpPost("v1/logout-all")]
    public async Task<ActionResult> LogoutAllAsync()
    {
        // Get user ID from middleware (already validated and cached)
        var userId = CurrentUserId;
        if (userId == null)
        {
            return Unauthorized(new { error = "Authentication failed", code = "AUTH_E007" });
        }

        // Revoke all refresh tokens for the user
        await _refreshTokenService.RevokeAllUserRefreshTokensAsync(userId.Value);

        return Ok(new { message = "Logout from all devices successful" });
    }
}