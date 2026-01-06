using RealtorApp.Contracts.Commands.Invitations.Requests;
using RealtorApp.Contracts.Commands.Invitations.Responses;
using RealtorApp.Infra.Data;

namespace RealtorApp.Domain.Interfaces;

public interface IInvitationService
{
    Task<SendInvitationCommandResponse> SendClientInvitationsAsync(SendInvitationCommand command, long agentUserId);
    Task<ValidateInvitationResponse> ValidateClientInvitationAsync(Guid invitationToken);
    Task<AcceptInvitationCommandResponse> AcceptClientInvitationAsync(AcceptInvitationCommand command);
    Task<ResendInvitationCommandResponse> ResendClientInvitationAsync(ResendInvitationCommand command, long agentUserId);
    Task<ValidateTeammateInvitationResponse> ValidateTeammateInvitationAsync(Guid invitationToken);
    Task<AcceptInvitationCommandResponse> AcceptTeammateInvitationAsync(AcceptInvitationCommand command);
    Task<SendTeammateInvitationCommandResponse> SendTeammateInvitationsAsync(SendTeammateInvitationCommand command, long invitingAgentId);
}
