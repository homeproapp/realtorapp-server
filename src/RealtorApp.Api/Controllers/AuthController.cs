using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
    IUserService userService,
    AppSettings appSettings) : BaseController
{
    private readonly IJwtService _jwtService = jwtService;
    private readonly IRefreshTokenService _refreshTokenService = refreshTokenService;
    private readonly IUserAuthService _userAuthService = userAuthService;
    private readonly IAuthProviderService _authProviderService = authProviderService;
    private readonly IUserService _userService = userService;
    private readonly AppSettings _appSettings = appSettings;

    [AllowAnonymous]
    [HttpPost("v1/login")]
    [EnableRateLimiting("Anonymous")]
    public async Task<ActionResult<LoginCommandResponse>> LoginAsync([FromBody] LoginCommand command)
    {
        // Validate Firebase token
        var firebaseUser = await _authProviderService.ValidateTokenAsync(command.FirebaseToken);
        if (firebaseUser == null)
        {
            return Unauthorized(new { error = "Authentication failed", code = "AUTH_E009" });
        }

        // Get or create agent user (idempotent)
        var user = await _userService.GetOrCreateAgentUserAsync(firebaseUser.Uid, firebaseUser.Email, firebaseUser.DisplayName);
        var role = user.Agent == null ? "client" : "agent";

        if (!user.Uuid.HasValue)
        {
            return BadRequest();
        }


        var accessToken = _jwtService.GenerateAccessToken(user.Uuid.Value, role);
        var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(user.UserId);

        return Ok(new LoginCommandResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _appSettings.Jwt.AccessTokenExpirationMinutes * 60
        });
    }

    [HttpPost("v1/refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("Anonymous")]
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
    [EnableRateLimiting("Authenticated")]
    public async Task<ActionResult> LogoutAsync([FromBody] LogoutCommand command)
    {
        // Revoke the specific refresh token
        await _refreshTokenService.RevokeRefreshTokenAsync(command.RefreshToken);

        return Ok(new { message = "Logout successful" });
    }

    [HttpPost("v1/logout-all")]
    [EnableRateLimiting("Authenticated")]
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