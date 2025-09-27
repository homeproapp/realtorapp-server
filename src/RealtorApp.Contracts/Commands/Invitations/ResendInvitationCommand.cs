namespace RealtorApp.Contracts.Commands.Invitations;

public class ResendInvitationCommand
{
    public long ClientInvitationId { get; set; }
    public ClientInvitationUpdateRequest ClientDetails { get; set; } = null!;
}