namespace RealtorApp.Domain.Interfaces;

public interface IUserAuthService
{
    Task<long?> GetUserIdByUuid(Guid uuid);
    Task<bool> IsConversationParticipant(long userId, long conversationId);
    Task<bool> UserIsAssignedToProperty(long userId, long propertyId);
    Task<bool> IsAgentEmailValidated(long userId);
}
