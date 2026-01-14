namespace RealtorApp.Contracts.Commands.Invitations.Requests;

public class AcceptInvitationCommand
{
    public string? EnteredFirstName { get; set; }
    public string? EnteredLastName { get; set; }
    public required string InvitationToken { get; set; }
    public required string Password { get; set; }
}

/// <summary>
/// Used when the user is already authenticated in the app
/// </summary>
public class AcceptInvitationWithTokenCommand
{
    public required string InvitationToken { get; set; }
}
