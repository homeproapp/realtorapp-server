namespace RealtorApp.Domain.Interfaces;

public interface IUserAuthService
{
    Task<long?> GetUserIdByUuid(Guid uuid);
    Task<bool> IsConversationParticipant(long userId, long conversationId);
    Task<bool> UserIsConnectedToListing(long userId, long listingId);
    Task<bool> IsAgentEmailValidated(long userId);
}
