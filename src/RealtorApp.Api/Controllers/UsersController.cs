using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealtorApp.Contracts.Queries.User.Responses;
using RealtorApp.Domain.Interfaces;

namespace RealtorApp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/Users/v1")]
public class UsersController(IUserService userService) : RealtorApiBaseController
{
    private readonly IUserService _userService = userService;

    [HttpGet("me")]
    public async Task<ActionResult<UserProfileQueryResponse>> GetMyProfile()
    {
        var result = await _userService.GetUserProfileAsync(RequiredCurrentUserId);

        if (result == null)
        {
            return BadRequest(new { errorCode = "USER_E001", message = "User not found" });
        }

        return Ok(result);
    }
}
