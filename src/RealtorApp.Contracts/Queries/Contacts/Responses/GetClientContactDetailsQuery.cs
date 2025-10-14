using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Queries.Contacts.Responses;

public class GetClientContactDetailsQuery : ResponseWithError
{

}

public class ClientContactDetailsResponse
{
    public required long ContactId { get; set; }
    public long? UserId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public bool HasAcceptedInvite { get; set; }
    public bool InviteHasExpired { get; set; }
    public required ClientContantListingDetailsResponse[] Listings { get; set; }
    public ClientContactAssociatedUsers[] AssociatedWith { get; set; } = [];
}

public class ClientContantListingDetailsResponse
{
    public long ListingInvitationId { get; set; }
    public long? ListingId { get; set; }
    public bool IsActive { get; set; }
    //TODO: Finish this!
}

public class ClientContactAssociatedUsers
{
    public required long ContactId { get; set; }
    public long? UserId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public bool HasAcceptedInvite { get; set; }
    public bool InviteHasExpired { get; set; }
}