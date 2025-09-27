namespace RealtorApp.Domain.DTOs;

public class InvitationEmailDto
{
    public required string ClientEmail { get; set; }
    public string? ClientFirstName { get; set; }
    public required string AgentName { get; set; }
    public required string EncryptedData { get; set; }
}