namespace RealtorApp.Contracts.Commands.Invitations;

public class ClientInvitationRequest
{
    public required string Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
}