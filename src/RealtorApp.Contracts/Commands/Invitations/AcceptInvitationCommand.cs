namespace RealtorApp.Contracts.Commands.Invitations;

public class AcceptInvitationCommand
{
    public required string InvitationToken { get; set; }
    public required string Password { get; set; }
}