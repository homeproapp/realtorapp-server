using RealtorApp.Contracts.Commands.Invitations.Requests;
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
          TeammateFirstName = request.FirstName,
          TeammateLastName = request.LastName,
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
}
