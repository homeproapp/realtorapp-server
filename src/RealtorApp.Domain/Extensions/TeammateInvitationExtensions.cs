using RealtorApp.Contracts.Commands.Invitations.Requests;
using RealtorApp.Contracts.Commands.Invitations.Responses;
using RealtorApp.Contracts.Enums;
using RealtorApp.Domain.DTOs;
using RealtorApp.Infra.Data;

namespace RealtorApp.Domain.Extensions;

public static class TeammateInvitationExtensions
{
    public static TeammateInvitation ToTeammateInvitation(this InviteTeammatesInfoRequest request, long listingId, long invitedById)
    {
        return new()
        {
          TeammateEmail = request.Email,
          TeammateFirstName = request.FirstName ?? string.Empty,
          TeammateLastName = request.LastName ?? string.Empty,
          TeammatePhone = request.Phone,
          InvitedListingId = listingId,
          InvitedBy = invitedById,
          InvitationToken = Guid.NewGuid(),
        };
    }

    public static TeammateInvitationEmailDto ToDto(this TeammateInvitation teammateInvitation,
        string encryptedData,
        string agentName,
        bool existingUser
        )
    {
        return new()
        {
            TeammateEmail = teammateInvitation.TeammateEmail,
            TeammateFirstName = teammateInvitation.TeammateFirstName,
            TeammateInvitationId = teammateInvitation.TeammateInvitationId,
            EncryptedData = encryptedData,
            AgentName = agentName,
            IsExistingUser = existingUser
        };
    }

    public static bool IsValid(this TeammateInvitation? invitation)
    {
        if (invitation == null ||
            invitation.TeammateRoleType == (short)TeammateTypes.Unknown ||
            invitation.AcceptedAt.HasValue ||
            invitation.DeletedAt.HasValue ||
            invitation.ExpiresAt < DateTime.UtcNow)
        {
            return false;
        }

        return true;
    }

    public static User ToTeammateUserByType(this TeammateInvitation invitation, AcceptInvitationCommand command, string uuidString)
    {
        var user = new User()
        {
            Uuid = uuidString,
            Email = invitation.TeammateEmail,
            FirstName = command.EnteredFirstName!,
            LastName = command.EnteredLastName!,
            Phone = invitation.TeammatePhone
        };

        var invitedRoleType = (TeammateTypes)invitation.TeammateRoleType;

        if (invitedRoleType == TeammateTypes.Agent)
        {
            user.Agent = new(); // TODO: can check for other agent specific details here.
        }

        return user;
    }

    public static ValidateTeammateInvitationResponse ToValidateInvitationResponse(this TeammateInvitation invitation)
    {
        return new ValidateTeammateInvitationResponse
        {
            IsValid = invitation.IsValid(),
            TeammateEmail = invitation.TeammateEmail,
            TeammateFirstName = invitation.TeammateFirstName,
            TeammateLastName = invitation.TeammateLastName,
            TeammatePhone = invitation.TeammatePhone,
            ExpiresAt = invitation.ExpiresAt,
            InvitingAgentFirstName = invitation.InvitedByNavigation.User.FirstName,
            InvitingAgentLastName = invitation.InvitedByNavigation.User.LastName,
            Property = new ValidateInvitationResponseProperties()
            {
                AddressLine1 = invitation.InvitedListing.Property.AddressLine1 ?? string.Empty,
                AddressLine2 = invitation.InvitedListing.Property.AddressLine2 ?? string.Empty,
                City = invitation.InvitedListing.Property.City ?? string.Empty,
                PostalCode = invitation.InvitedListing.Property.PostalCode ?? string.Empty,
                Region = invitation.InvitedListing.Property.Region ?? string.Empty,
                CountryCode = invitation.InvitedListing.Property.CountryCode ?? string.Empty
            },
        };
    }
}
