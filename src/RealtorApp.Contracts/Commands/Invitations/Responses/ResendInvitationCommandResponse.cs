using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Commands.Invitations.Responses;

public class ResendInvitationCommandResponse : ResponseWithError
{
    public bool Success { get; set; }
}
