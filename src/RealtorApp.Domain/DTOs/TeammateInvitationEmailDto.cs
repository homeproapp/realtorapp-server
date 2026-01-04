namespace RealtorApp.Domain.DTOs;

public class TeammateInvitationEmailDto
{
    public long TeammateInvitationId { get; set; }
    public required string TeammateEmail { get; set; }
    public string? TeammateFirstName { get; set; }
    public required string AgentName { get; set; }
    public required string EncryptedData { get; set; }
    public required bool IsExistingUser { get; set; }
}
