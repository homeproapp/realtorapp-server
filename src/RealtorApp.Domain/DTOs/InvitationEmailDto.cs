namespace RealtorApp.Domain.DTOs;

public class InvitationEmailDto
{
    public long ClientInvitationId { get; set; }
    public required string ClientEmail { get; set; }
    public string? ClientFirstName { get; set; }
    public required string AgentName { get; set; }
    public required string EncryptedData { get; set; }
    public required bool IsExistingUser { get; set; }
}
