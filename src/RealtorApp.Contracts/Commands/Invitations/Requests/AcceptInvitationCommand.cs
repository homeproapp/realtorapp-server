namespace RealtorApp.Contracts.Commands.Invitations.Requests;

public class AcceptInvitationCommand
{
    public string? EnteredFirstName { get; set; }
    public string? EnteredLastName { get; set; }
    public required string InvitationToken { get; set; }
    public required string Password { get; set; }
}
