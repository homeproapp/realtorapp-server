using RealtorApp.Domain.DTOs;

namespace RealtorApp.Domain.Interfaces;

public interface IEmailService
{
    Task<bool> SendInvitationEmailAsync(string clientEmail, string? clientFirstName, string agentName, string encryptedData, bool isExistingUser);
    Task<List<InvitationEmailDto>> SendBulkInvitationEmailsAsync(List<InvitationEmailDto> invitations);
}