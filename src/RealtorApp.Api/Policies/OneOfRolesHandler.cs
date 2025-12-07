using Microsoft.AspNetCore.Authorization;

namespace RealtorApp.Api.Policies;

public class OneOfRolesHandler : AuthorizationHandler<OneOfRolesRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OneOfRolesRequirement requirement)
    {
        foreach (string role in requirement.Roles)
        {
            if (context.User.IsInRole(role))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        context.Fail();
        return Task.CompletedTask;
    }
}
