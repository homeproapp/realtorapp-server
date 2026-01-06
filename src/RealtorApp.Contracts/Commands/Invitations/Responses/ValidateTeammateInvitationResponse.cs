using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Commands.Invitations.Responses;

public class ValidateTeammateInvitationResponse : ResponseWithError
{
    public bool IsValid { get; set; }
    public string? TeammateEmail { get; set; }
    public string? TeammateFirstName { get; set; }
    public string? TeammateLastName { get; set; }
    public string? TeammatePhone { get; set; }
    public string InvitingAgentFirstName { get; set; } = string.Empty;
    public string InvitingAgentLastName { get; set; } = string.Empty;
    public bool IsExistingUser { get; set; }
    public ValidateInvitationResponseProperties Property { get; set; } = new();
    public DateTime? ExpiresAt { get; set; }
}
