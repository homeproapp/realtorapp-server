using RealtorApp.Contracts.Enums;

namespace RealtorApp.Contracts.Commands.Invitations.Requests;

public class SendTeammateInvitationCommand
{
    public long ListingId { get; set; }
    public InviteTeammatesInfoRequest[] Teammates { get; set; } = [];
}

public class InviteTeammatesInfoRequest
{
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Phone { get; set; }
    public TeammateTypes Type { get; set; }
}
