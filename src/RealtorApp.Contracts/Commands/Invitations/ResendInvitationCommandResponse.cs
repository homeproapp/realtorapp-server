using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Commands.Invitations;

public class ResendInvitationCommandResponse : ResponseWithError
{
    public bool Success { get; set; }
}