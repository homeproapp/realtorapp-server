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

    public static bool IsValid(this ClientInvitation? invitation)
    {
        if (invitation == null ||
            invitation.AcceptedAt.HasValue ||
            invitation.DeletedAt.HasValue ||
            invitation.ExpiresAt < DateTime.UtcNow)
        {
            return false;
        }

        return true;
    }

    public static Client ToClientUser(this ClientInvitation invitation, string uuidString)
    {
        return new Client()
        {
            User = new()
            {
                Uuid = Guid.Parse(uuidString),
                Email = invitation.ClientEmail,
                FirstName = invitation.ClientFirstName,
                LastName = invitation.ClientLastName,
                Phone = invitation.ClientPhone
            }
        };
    }
}