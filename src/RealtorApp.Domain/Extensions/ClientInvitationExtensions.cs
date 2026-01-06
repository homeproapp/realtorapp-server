using RealtorApp.Domain.DTOs;
using RealtorApp.Infra.Data;
using RealtorApp.Contracts.Commands.Invitations;
using RealtorApp.Contracts.Commands.Invitations.Responses;
using RealtorApp.Contracts.Commands.Invitations.Requests;

namespace RealtorApp.Domain.Extensions;

public static class ClientInvitationExtensions
{
    public static InvitationEmailDto ToEmailDto(this ClientInvitation invitation, string agentName, string encryptedData, bool isExistingUser)
    {
        return new InvitationEmailDto
        {
            ClientInvitationId = invitation.ClientInvitationId,
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

    public static Client ToClientUser(this ClientInvitation invitation, AcceptInvitationCommand command, string uuidString)
    {
        return new Client()
        {
            User = new()
            {
                Uuid = uuidString,
                Email = invitation.ClientEmail,
                FirstName = command.EnteredFirstName ?? invitation.ClientFirstName,
                LastName = command.EnteredLastName ?? invitation.ClientLastName,
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
            AgentFirstName = invitation.InvitedByNavigation.User.FirstName,
            AgentLastName = invitation.InvitedByNavigation.User.LastName,
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
