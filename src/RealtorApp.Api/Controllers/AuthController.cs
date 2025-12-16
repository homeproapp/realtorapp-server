using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RealtorApp.Contracts.Commands.Auth;
using RealtorApp.Domain.Constants;
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
    AppSettings appSettings) : RealtorApiBaseController
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
    public async Task<ActionResult<TokenResponse>> LoginAsync([FromBody] LoginCommand command)
    {
        var firebaseUser = await _authProviderService.SignInWithEmailAndPasswordAsync(command.Email, command.Password);
        if (firebaseUser == null)
        {
            return Unauthorized(new { error = "Authentication failed", code = "AUTH_E009" });
        }

        var user = await _userService.GetOrCreateUserAsync(firebaseUser.Uid, firebaseUser.Email);

        if (user == null)
        {
            return BadRequest("Login failed");
        }

        var role = user.Agent == null ? RoleConstants.Client : RoleConstants.Agent;

        var accessToken = _jwtService.GenerateAccessToken(user.Uuid, role);
        var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(user.UserId);

        return Ok(new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _appSettings.Jwt.AccessTokenExpirationMinutes * 60
        });
    }

    [AllowAnonymous]
    [HttpPost("v1/register")]
    [EnableRateLimiting("Anonymous")]
    public async Task<ActionResult<TokenResponse>> RegisterAsync([FromBody] RegisterCommand command)
    {
        var firebaseUser = await _authProviderService.RegisterWithEmailAndPasswordAsync(
            command.Email,
            command.Password,
            emailVerified: false
        );

        if (firebaseUser == null)
        {
            return BadRequest(new { error = "Registration failed", code = "AUTH_E010" });
        }

        try
        {
            var displayName = $"{command.FirstName} {command.LastName}";
            var user = await _userService.GetOrCreateUserAsync(firebaseUser.Uid, firebaseUser.Email!, displayName);

            if (user == null)
            {
                await _authProviderService.DeleteUserAsync(firebaseUser.Uid);
                return BadRequest(new { error = "Registration failed", code = "AUTH_E013" });
            }

            var role = user.Agent == null ? RoleConstants.Client : RoleConstants.Agent;

            var accessToken = _jwtService.GenerateAccessToken(user.Uuid, role);
            var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(user.UserId);

            return Ok(new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = _appSettings.Jwt.AccessTokenExpirationMinutes * 60
            });
        }
        catch (Exception)
        {
            await _authProviderService.DeleteUserAsync(firebaseUser.Uid);
            throw;
        }
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

        var accessToken = _jwtService.GenerateAccessToken(userDto.UserUuid, userDto.Role);

        //TODO: do we need a new refresh if the refresh token isnt expired?
        var newRefreshToken = await _refreshTokenService.CreateRefreshTokenAsync(userDto.UserId);

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
