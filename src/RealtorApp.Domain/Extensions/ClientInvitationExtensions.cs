using RealtorApp.Domain.DTOs;
using RealtorApp.Domain.Models;
using RealtorApp.Contracts.Commands.Invitations;

namespace RealtorApp.Domain.Extensions;

public static class ClientInvitationExtensions
{
    public static InvitationEmailDto ToEmailDto(this ClientInvitation invitation, string agentName, string encryptedData, bool isExistingUser)
    {
        return new InvitationEmailDto
        {
            ClientEmail = invitation.ClientEmail,
            ClientFirstName = invitation.ClientFirstName,
            AgentName = agentName,
            EncryptedData = encryptedData,
            IsExistingUser = isExistingUser
        };
    }

    public static bool IsValid(this ClientInvitation? invitation)
    {
        if (invitation == null ||
            invitation.AcceptedAt.HasValue ||
            invitation.DeletedAt.HasValue ||
            invitation.ExpiresAt < DateTime.UtcNow ||
            invitation.ClientInvitationsProperties.Count == 0)
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

    public static ValidateInvitationResponse ToValidateInvitationResponse(this ClientInvitation invitation)
    {
        return new ValidateInvitationResponse
        {
            IsValid = invitation.IsValid(),
            ClientEmail = invitation.ClientEmail,
            ClientFirstName = invitation.ClientFirstName,
            ClientLastName = invitation.ClientLastName,
            ClientPhone = invitation.ClientPhone,
            ExpiresAt = invitation.ExpiresAt,
            Properties = invitation.ClientInvitationsProperties?.Select(p => new ValidateInvitationResponseProperties
            {
                AddressLine1 = p.PropertyInvitation?.AddressLine1 ?? string.Empty,
                AddressLine2 = p.PropertyInvitation?.AddressLine2 ?? string.Empty,
                City = p.PropertyInvitation?.City ?? string.Empty,
                PostalCode = p.PropertyInvitation?.PostalCode ?? string.Empty,
                Region = p.PropertyInvitation?.Region ?? string.Empty,
                CountryCode = p.PropertyInvitation?.CountryCode ?? string.Empty
            }).ToList() ?? []
        };
    }
}