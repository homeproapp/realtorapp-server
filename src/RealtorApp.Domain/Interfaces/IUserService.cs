using RealtorApp.Contracts.Queries.User.Responses;
using RealtorApp.Infra.Data;

namespace RealtorApp.Domain.Interfaces;

public interface IUserService
{
    Task<User?> GetOrCreateUserAsync(string firebaseUid, string email, string? displayName = null);
    Task<string?> GetAgentName(long agentId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<DashboardQueryResponse> GetAgentDashboard(long userId);
    Task<DashboardQueryResponse> GetClientDashboard(long userId);
    Task<UserProfileQueryResponse?> GetUserProfileAsync(long userId);
}
