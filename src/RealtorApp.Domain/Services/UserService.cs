using Microsoft.EntityFrameworkCore;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Models;

namespace RealtorApp.Domain.Services;

public class UserService(RealtorAppDbContext dbContext) : IUserService
{
    private readonly RealtorAppDbContext _dbContext = dbContext;

    public async Task<User> GetOrCreateAgentUserAsync(string firebaseUid, string email, string? displayName)
    {
        var userUuid = new Guid(firebaseUid);

        var existingUser = await _dbContext.Users
            .Include(u => u.Agent)
            .Include(u => u.Client)
            .FirstOrDefaultAsync(u => u.Uuid == userUuid);

        if (existingUser != null)
            return existingUser;

        var user = new User
        {
            Uuid = userUuid,
            Email = email,
            Agent = new(),
        };

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            var nameParts = displayName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            user.FirstName = nameParts.Length > 0 ? nameParts[0] : null;
            user.LastName = nameParts.Length > 1 ? nameParts[1] : null;
        }

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return user;
    }

    public async Task<string?> GetAgentName(long agentId)
    {
        return await _dbContext.Agents.Where(i => i.UserId == agentId)
            .Select(i => i.User.FirstName + " " + i.User.LastName)
            .FirstOrDefaultAsync();
    }
}