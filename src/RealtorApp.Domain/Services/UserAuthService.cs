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
        if (_cache.TryGetValue($"{_userIdByUuIdCachePrefix}{uuid}", out long cachedUserId))
        {
            return cachedUserId;
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Uuid == uuid)
            .ConfigureAwait(false);

        if (user == null)
        {
            return null;
        }

        _cache.Set($"{_userIdByUuIdCachePrefix}{uuid}", user.UserId, _userUuidEntryOptions);
        return user.UserId;
    }

    public async Task<bool> IsConversationParticipant(long userId, long conversationId, long propertyId)
    {
        if (_cache.TryGetValue($"{_conversationParticipantsCachePrefix}{conversationId}", out HashSet<long>? cachedParticipants))
        {
            return cachedParticipants!.Contains(userId);
        }

        var participants = await _context.ConversationsProperties
            .Where(i => i.ConversationId == conversationId && i.PropertyId == propertyId)
            .AsNoTracking()
            .SelectMany(i => new long[] { i.Conversation.AgentId }
                .Concat(i.Property.ClientsProperties.Select(cpp => cpp.ClientId)))
            .ToArrayAsync()
            .ConfigureAwait(false);

        _cache.Set($"{_conversationParticipantsCachePrefix}{conversationId}", new HashSet<long>(participants ?? []), _conversationParticipantsEntryOptions);

        return participants?.Contains(userId) ?? false;
    }

    public async Task<bool> UserIsAssignedToProperty(long userId, long propertyId)
    {
        if (_cache.TryGetValue($"{_userPropertyAssignmentCachePrefix}{propertyId}", out HashSet<long>? cachedUserIds))
        {
            return cachedUserIds!.Contains(userId);
        }

        var propertyUserIds = await _context.ClientsProperties
            .Where(i => i.PropertyId == propertyId)
            .AsNoTracking()
            .SelectMany(i => new long[] { i.AgentId, i.ClientId })
            .ToArrayAsync()
            .ConfigureAwait(false);

        _cache.Set($"{_userPropertyAssignmentCachePrefix}{propertyId}", new HashSet<long>(propertyUserIds ?? []), _userPropertyAssignmentEntryOptions);
        
        return propertyUserIds?.Contains(userId) ?? false;
    }

    public async Task<bool> IsAgentEmailValidated(long userId)
    {
        var user = await _context.Agents.FindAsync(userId).ConfigureAwait(false);

        return user?.EmailValidated ?? false;
    }
}