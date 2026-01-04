namespace RealtorApp.Contracts.Commands.Invitations.Requests;

public class ResendInvitationCommand
{
    public long ClientInvitationId { get; set; }
    public ClientInvitationUpdateRequest ClientDetails { get; set; } = null!;
}
