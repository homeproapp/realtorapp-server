using RealtorApp.Contracts.Common;
using RealtorApp.Contracts.Queries.Listing.Responses;

namespace RealtorApp.Contracts.Commands.Invitations.Responses;

public class SendTeammateInvitationCommandResponse : ResponseWithError
{
    public int InvitationsSent { get; set; }
    public PendingTeammateInvitation[] PendingTeamInvitations { get; set; } = [];
}
