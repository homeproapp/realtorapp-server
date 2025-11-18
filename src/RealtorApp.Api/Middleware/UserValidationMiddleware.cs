using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using RealtorApp.Domain.Interfaces;

namespace RealtorApp.Api.Middleware;

public class UserValidationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IUserAuthService userAuthService)
    {
        // Skip validation if endpoint allows anonymous access
        var endpoint = context.GetEndpoint();
        var allowAnonymous = endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null;

        if (allowAnonymous)
        {
            await next(context);
            return;
        }

        var userUuidClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userUuidClaim))
        {
            await WriteErrorResponseAsync(context, "AUTH_E001");
            return;
        }

        var userId = await userAuthService.GetUserIdByUuid(userUuidClaim);
        if (userId == null)
        {
            await WriteErrorResponseAsync(context, "AUTH_E002");
            return;
        }

        var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(roleClaim) || (roleClaim != "agent" && roleClaim != "client"))
        {
            await WriteErrorResponseAsync(context, "AUTH_E003");
            return;
        }

        context.Items["UserId"] = userId.Value;

        await next(context);
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, string errorCode)
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";

        var response = new { error = "Authentication failed", code = errorCode };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}