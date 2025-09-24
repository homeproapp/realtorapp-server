using RealtorApp.Contracts.Commands.Invitations;
using RealtorApp.Domain.Models;

namespace RealtorApp.Domain.Interfaces;

public interface IInvitationService
{
    Task<SendInvitationCommandResponse> SendInvitationsAsync(SendInvitationCommand command, long agentUserId);
    Task<ValidateInvitationResponse> ValidateInvitationAsync(Guid invitationToken);
    Task<AcceptInvitationCommandResponse> AcceptInvitationAsync(AcceptInvitationCommand command);
}