using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RealtorApp.Contracts.Queries.User.Responses;
using RealtorApp.Domain.Constants;
using RealtorApp.Domain.Interfaces;

namespace RealtorApp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting(RateLimitConstants.Authenticated)]
public class UsersController(IUserService userService) : RealtorApiBaseController
{
    private readonly IUserService _userService = userService;

    [HttpGet("v1/me")]
    public async Task<ActionResult<UserProfileQueryResponse>> GetMyProfile()
    {
        var result = await _userService.GetUserProfileAsync(RequiredCurrentUserId);

        if (result == null)
        {
            return BadRequest(new { errorCode = "USER_E001", message = "User not found" });
        }

        return Ok(result);
    }

    [HttpGet("v1/dashboard")]
    public async Task<ActionResult<DashboardQueryResponse>> GetAgentDashboard()
    {
        DashboardQueryResponse? result = null;
        if (CurrentUserRole == RoleConstants.Client)
        {
            result = await _userService.GetClientDashboard(RequiredCurrentUserId);
        }
        else if (CurrentUserRole == RoleConstants.Agent)
        {
            result = await _userService.GetAgentDashboard(RequiredCurrentUserId);
        };

        if (result == null)
        {
            return BadRequest("Something went wrong");
        }

        return Ok(result);
    }
}
