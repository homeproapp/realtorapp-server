using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Commands.Invitations;

public class ValidateInvitationResponse : ResponseWithError
{
    public bool IsValid { get; set; }
    public string? ClientEmail { get; set; }
    public string? ClientFirstName { get; set; }
    public string? ClientLastName { get; set; }
    public string? ClientPhone { get; set; }
    public bool IsExistingUser { get; set; }
    public List<ValidateInvitationResponseProperties> Properties { get; set; } = [];
    public DateTime? ExpiresAt { get; set; }
}