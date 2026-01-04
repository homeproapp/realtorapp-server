using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Commands.Invitations.Responses;

public class SendTeammateInvitationCommandResponse : ResponseWithError
{
    public int InvitationsSent { get; set; }
}
