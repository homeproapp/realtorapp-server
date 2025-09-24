using RealtorApp.Domain.DTOs;
using RealtorApp.Domain.Models;

namespace RealtorApp.Domain.Extensions;

public static class ClientInvitationExtensions
{
    public static InvitationEmailDto ToEmailDto(this ClientInvitation invitation, string agentName, bool isExistingUser = false)
    {
        return new InvitationEmailDto
        {
            ClientEmail = invitation.ClientEmail,
            ClientFirstName = invitation.ClientFirstName,
            InvitationToken = invitation.InvitationToken,
            AgentName = agentName,
            IsExistingUser = isExistingUser
        };
    }
}