namespace RealtorApp.Domain.Interfaces;

public interface IUserAuthService
{
    Task<long?> GetUserIdByUuid(string uuid);
    Task<bool> IsConversationParticipant(long userId, long conversationId);
    Task<bool> UserIsConnectedToListing(long userId, long listingId);
    Task<bool> IsAgentEmailValidated(long userId);
    Task<long[]> GetUserToListingIdsByUserId(long userId);
    Task<HashSet<long>> GetUsersAssignedToListing(long listingId);
    void InvalidateConversationParticipantsCache(long conversationId);
    void InvalidateUserIsConnectedToListingCache(long listingId);
    void InvalidateUserToListingIdsCache(long userId);
}
