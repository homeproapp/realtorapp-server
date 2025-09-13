using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RealtorApp.Domain.Models;
using RealtorApp.Domain.Settings;

namespace RealtorApp.Domain.Services;

public class UserAuthService(IMemoryCache cache, RealtorAppDbContext context, AppSettings appSettings)
{
    private readonly IMemoryCache _cache = cache;
    private readonly RealtorAppDbContext _context = context;
    private readonly AppSettings _appSettings = appSettings;
    private const string _userUuIdCachePrefix = "uuid:";
    private const string _conversationParticipantsCachePrefix = "participants:";
    private readonly MemoryCacheEntryOptions _userUuidEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(appSettings.UserIdCacheExpirationInMins)
    };

    private readonly MemoryCacheEntryOptions _conversationParticipantsEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(appSettings.ConversationParticipantsCacheExpirationInMins)
    };


    public async Task<long?> GetUserIdByUuid(Guid uuid)
    {
        if (_cache.TryGetValue($"{_userUuIdCachePrefix}{uuid}", out long cachedUserId))
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

        _cache.Set($"{_userUuIdCachePrefix}{uuid}", user.UserId, _userUuidEntryOptions);
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
            .SelectMany(i => new long[] { i.Conversation.AgentId }
                .Concat(i.Property.ClientsProperties.Select(cpp => cpp.ClientId)))
            .ToArrayAsync()
            .ConfigureAwait(false);

        if (participants == null || participants.Length == 0)
        {
            return false;
        }

        _cache.Set($"{_conversationParticipantsCachePrefix}{conversationId}", new HashSet<long>(participants), _conversationParticipantsEntryOptions);

        return participants.Contains(userId);
    }

    public async Task<bool> UserIsAssignedToProperty(long userId, long propertyId)
    {
        //TODO
    }

    public async Task<bool> IsAgentEmailValidated(long userId)
    {
        var user = await _context.Agents.FindAsync(userId).ConfigureAwait(false);

        return user?.EmailValidated ?? false;
    }
}