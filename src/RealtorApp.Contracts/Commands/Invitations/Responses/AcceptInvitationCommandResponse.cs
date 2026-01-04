using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Commands.Invitations.Responses;

public class AcceptInvitationCommandResponse : ResponseWithError
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
}
