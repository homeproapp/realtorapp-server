using RealtorApp.Domain.Models;

namespace RealtorApp.Domain.Interfaces;

public interface IUserService
{
    Task<User> GetOrCreateAgentUserAsync(string firebaseUid, string email, string? displayName);
    Task<string?> GetAgentName(long agentId);
}