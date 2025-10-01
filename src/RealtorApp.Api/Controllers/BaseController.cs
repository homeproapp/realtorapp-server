using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RealtorApp.Domain.Constants;

namespace RealtorApp.Api.Controllers;

public abstract class RealtorApiBaseController : ControllerBase
{
    protected Guid? CurrentUserUuid
    {
        get
        {
            var userUuidClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userUuidClaim) || !Guid.TryParse(userUuidClaim, out var userUuid))
            {
                return null;
            }
            return userUuid;
        }
    }

    protected string? CurrentUserRole
    {
        get
        {
            var roleClaim = User.FindFirst(RoleConstants.Claim)?.Value;
            
            if (string.IsNullOrEmpty(roleClaim))
            {
                return null;
            }

            return roleClaim;
        }
    }

    protected Guid RequiredCurrentUserUuid
    {
        get
        {
            return CurrentUserUuid ?? throw new UnauthorizedAccessException("User UUID not found in claims");
        }
    }

    protected long? CurrentUserId
    {
        get
        {
            if (HttpContext.Items.TryGetValue("UserId", out var userIdObj) && userIdObj is long userId)
            {
                return userId;
            }
            return null;
        }
    }

    protected long RequiredCurrentUserId
    {
        get
        {
            return CurrentUserId ?? throw new UnauthorizedAccessException("User ID not found in context");
        }
    }
}