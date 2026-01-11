using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Commands.Invitations.Responses;

public class RemoveTeammateInvitationResponse : ResponseWithError
{
    public bool Success { get; set; }
}
