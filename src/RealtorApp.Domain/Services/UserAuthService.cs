using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Models;
using RealtorApp.Domain.Settings;

namespace RealtorApp.Domain.Services;

public class UserAuthService(IMemoryCache cache, RealtorAppDbContext context, AppSettings appSettings) : IUserAuthService
{
    private readonly IMemoryCache _cache = cache;
    private readonly RealtorAppDbContext _context = context;
    private readonly AppSettings _appSettings = appSettings;
    private const string _userIdByUuIdCachePrefix = "uuid:";
    private const string _userPropertyAssignmentCachePrefix = "property:";
    private const string _conversationParticipantsCachePrefix = "participants:";
    private readonly MemoryCacheEntryOptions _userUuidEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(appSettings.UserIdCacheExpirationInMins)
    };

    private readonly MemoryCacheEntryOptions _conversationParticipantsEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(appSettings.ConversationParticipantsCacheExpirationInMins)
    };

    private readonly MemoryCacheEntryOptions _userPropertyAssignmentEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(appSettings.UsersAssignedToPropertyCacheExpirationInMins)
    };


    public async Task<long?> GetUserIdByUuid(Guid uuid)
    {
        return await _cache.GetOrCreateAsync(
            $"{_userIdByUuIdCachePrefix}{uuid}",
            async entry =>
            {
                entry.SetOptions(_userUuidEntryOptions);

                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Uuid == uuid);

                return user?.UserId;
            });
    }

    public async Task<bool> IsConversationParticipant(long userId, long conversationId)
    {
        var participants = await _cache.GetOrCreateAsync(
            $"{_conversationParticipantsCachePrefix}{conversationId}",
            async entry =>
            {
                entry.SetOptions(_conversationParticipantsEntryOptions);

                var clientsPropertiesInConversation = await _context.Conversations
                    .Where(i => i.ConversationId == conversationId)
                    .AsNoTracking()
                    .SelectMany(i => i.ClientsProperties.Select(x => new { x.AgentId, x.ClientId }))
                    .ToArrayAsync();

                long[]? ids = null;

                if (clientsPropertiesInConversation.Length > 0)
                {
                    var agentId = clientsPropertiesInConversation.FirstOrDefault()!.AgentId;
                    var clientIds = clientsPropertiesInConversation.Select(i => i.ClientId);
                    ids = [agentId, .. clientIds];
                }

                return new HashSet<long>(ids ?? []);
            });

        return participants?.Contains(userId) ?? false;
    }

    public async Task<bool> UserIsAssignedToProperty(long userId, long propertyId)
    {
        var assignedUserIds = await _cache.GetOrCreateAsync(
            $"{_userPropertyAssignmentCachePrefix}{propertyId}",
            async entry =>
            {
                entry.SetOptions(_userPropertyAssignmentEntryOptions);

                var propertyUserIds = await _context.ClientsProperties
                    .Where(i => i.PropertyId == propertyId)
                    .AsNoTracking()
                    .SelectMany(i => new long[] { i.AgentId, i.ClientId })
                    .ToArrayAsync();

                return new HashSet<long>(propertyUserIds ?? []);
            });

        return assignedUserIds?.Contains(userId) ?? false;
    }

    public async Task<bool> IsAgentEmailValidated(long userId)
    {
        var user = await _context.Agents.FindAsync(userId);

        return user?.EmailValidated ?? false;
    }
}