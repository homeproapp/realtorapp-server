namespace RealtorApp.Contracts.Commands.Invitations;

public class ClientInvitationRequest
{
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Phone { get; set; }
}