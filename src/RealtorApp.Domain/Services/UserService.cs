using Microsoft.EntityFrameworkCore;
using RealtorApp.Contracts.Queries.User.Responses;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Models;

namespace RealtorApp.Domain.Services;

public class UserService(RealtorAppDbContext dbContext) : IUserService
{
    private readonly RealtorAppDbContext _dbContext = dbContext;

    public async Task<User> GetOrCreateAgentUserAsync(string firebaseUid, string email, string? displayName)
    {

      var existingUser = await _dbContext.Users
          .Where(u => u.Uuid == firebaseUid)
          .Select(u => new User
          {
              UserId = u.UserId,
              Uuid = u.Uuid,
              Agent = u.Agent != null ? new Agent { UserId = u.UserId } : null,
              Client = u.Client != null ? new Client { UserId = u.UserId } : null
          })
          .FirstOrDefaultAsync();


        if (existingUser != null)
            return existingUser;

        var user = new User
        {
            Uuid = firebaseUid,
            Email = email,
            Agent = new(),
        };

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            var nameParts = displayName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            user.FirstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
            user.LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;
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

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<UserProfileQueryResponse?> GetUserProfileAsync(long userId)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new UserProfileQueryResponse
            {
                UserId = u.UserId,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Phone = u.Phone,
                ProfileImageId = u.ProfileImageId,
                Role = u.Agent != null ? "agent" : u.Client != null ? "client" : "unknown"
            })
            .FirstOrDefaultAsync();

        return user;
    }
}