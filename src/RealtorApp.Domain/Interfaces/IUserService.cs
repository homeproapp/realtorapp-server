using RealtorApp.Contracts.Queries.User.Responses;
using RealtorApp.Domain.Models;

namespace RealtorApp.Domain.Interfaces;

public interface IUserService
{
    Task<User> GetOrCreateAgentUserAsync(string firebaseUid, string email, string? displayName);
    Task<string?> GetAgentName(long agentId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<DashboardQueryResponse> GetAgentDashboard(long userId);
    Task<UserProfileQueryResponse?> GetUserProfileAsync(long userId);
}