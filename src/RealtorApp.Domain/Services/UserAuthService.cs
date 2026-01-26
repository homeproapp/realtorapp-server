using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Infra.Data;
using RealtorApp.Domain.Settings;

namespace RealtorApp.Domain.Services;

public class UserAuthService(IMemoryCache cache, RealtorAppDbContext context, AppSettings appSettings) : IUserAuthService
{
    private readonly IMemoryCache _cache = cache;
    private readonly RealtorAppDbContext _context = context;
    private readonly AppSettings _appSettings = appSettings;
    private const string _userIdByUuIdCachePrefix = "uuid:";
    private const string _listingIdToUserIdsCachePrefix = "listing:";
    private const string _userIdToListingIdsCachePrefix = "user-listings:";
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


    public async Task<long?> GetUserIdByUuid(string uuid)
    {
        return await _cache.GetOrCreateAsync(
            $"{_userIdByUuIdCachePrefix}{uuid}",
            async entry =>
            {
                entry.SetOptions(_userUuidEntryOptions);

                var user = await _context.Users
                    .Where(i => i.Uuid == uuid)
                    .AsNoTracking()
                    .Select(i => new
                    {
                        i.UserId
                    })
                    .FirstOrDefaultAsync();

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

                var ids = await _context.Conversations
                    .Where(i => i.ListingId == conversationId)
                    .SelectMany(i => i.Listing.AgentsListings
                        .Select(al => al.AgentId)
                        .Union(i.Listing.ClientsListings
                            .Select(cl => cl.ClientId)))
                    .ToArrayAsync();

                return new HashSet<long>(ids ?? []);
            });

        return participants?.Contains(userId) ?? false;
    }

    public void InvalidateConversationParticipantsCache(long conversationId)
    {
        _cache.Remove($"{_conversationParticipantsCachePrefix}{conversationId}");
    }

    public void InvalidateUserIsConnectedToListingCache(long listingId)
    {
        _cache.Remove($"{_listingIdToUserIdsCachePrefix}{listingId}");
    }


    public void InvalidateUserToListingIdsCache(long userId)
    {
        _cache.Remove($"{_userIdToListingIdsCachePrefix}{userId}");
    }

    public async Task<bool> UserIsConnectedToListing(long userId, long listingId)
    {
        var assignedUserIds = await _cache.GetOrCreateAsync(
            $"{_listingIdToUserIdsCachePrefix}{listingId}",
            async entry =>
            {
                entry.SetOptions(_userPropertyAssignmentEntryOptions);

                var listingUserIds = await _context.Listings
                    .Where(i => i.ListingId == listingId)
                    .AsNoTracking()
                    .SelectMany(i => i.AgentsListings
                        .Select(al => al.AgentId)
                        .Union(i.ClientsListings
                            .Select(cl => cl.ClientId)))
                    .ToArrayAsync();

                return new HashSet<long>(listingUserIds ?? []);
            });

        return assignedUserIds?.Contains(userId) ?? false;
    }

    public async Task<HashSet<long>> GetUsersAssignedToListing(long listingId)
    {
        var assignedUserIds = await _cache.GetOrCreateAsync(
            $"{_listingIdToUserIdsCachePrefix}{listingId}",
            async entry =>
            {
                entry.SetOptions(_userPropertyAssignmentEntryOptions);

                var listingUserIds = await _context.Conversations
                    .Where(i => i.ListingId == listingId)
                    .AsNoTracking()
                    .SelectMany(i => i.Listing.AgentsListings
                        .Select(al => al.AgentId)
                        .Union(i.Listing.ClientsListings
                            .Select(cl => cl.ClientId)))
                    .ToArrayAsync();

                return new HashSet<long>(listingUserIds ?? []);
            });

        return assignedUserIds ?? [];
    }

    public async Task<long[]> GetUserToListingIdsByUserId(long userId)
    {
        var usersListingIds = await _cache.GetOrCreateAsync(
            $"{_userIdToListingIdsCachePrefix}{userId}",
            async entry =>
            {
                entry.SetOptions(_userPropertyAssignmentEntryOptions);

                var listingUserIds = await _context.Listings
                    .Where(i => i.AgentsListings.Any(x => x.AgentId == userId) || i.ClientsListings.Any(x => x.ClientId == userId))
                    .AsNoTracking()
                    .Select(i => i.ListingId)
                    .ToArrayAsync();

                return listingUserIds ?? [];
            });

        return usersListingIds ?? [];
    }

    public async Task<bool> IsAgentEmailValidated(long userId)
    {
        var user = await _context.Agents.FindAsync(userId);

        return user?.EmailValidated ?? false;
    }
}
