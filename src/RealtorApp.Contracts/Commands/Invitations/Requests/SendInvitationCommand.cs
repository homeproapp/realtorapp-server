namespace RealtorApp.Contracts.Commands.Invitations.Requests;

public class SendInvitationCommand
{
    public required List<ClientInvitationRequest> Clients { get; set; }
    public required List<PropertyInvitationRequest> Properties { get; set; }
}
