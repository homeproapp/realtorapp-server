using Microsoft.AspNetCore.Authorization;

namespace RealtorApp.Api.Policies;

public class OneOfRolesRequirement(IEnumerable<string> requiredRoles) : IAuthorizationRequirement
{
    public IEnumerable<string> Roles { get; } = requiredRoles;
}
