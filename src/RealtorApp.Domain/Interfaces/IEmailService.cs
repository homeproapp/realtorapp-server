using RealtorApp.Domain.DTOs;

namespace RealtorApp.Domain.Interfaces;

public interface IEmailService
{
    Task<bool> SendInvitationEmailAsync(string clientEmail, string clientFirstName, Guid invitationToken, string agentName, bool IsExistingUser);
    Task<List<InvitationEmailDto>> SendBulkInvitationEmailsAsync(List<InvitationEmailDto> invitations);
}