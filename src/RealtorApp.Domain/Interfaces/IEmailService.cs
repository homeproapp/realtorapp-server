using RealtorApp.Domain.DTOs;
using RealtorApp.Infra.Data;

namespace RealtorApp.Domain.Interfaces;

public interface IEmailService
{
    Task<bool> SendClientInvitationEmailAsync(string clientEmail, string? clientFirstName, string agentName, string encryptedData, bool isExistingUser);
    Task<List<InvitationEmailDto>> SendClientBulkInvitationEmailsAsync(List<InvitationEmailDto> invitations);
    Task<int> SendTeammateBulkInvitationEmailsAsync(TeammateInvitationEmailDto[] invitations);
}
