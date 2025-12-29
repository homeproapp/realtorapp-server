using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Queries.Contacts.Responses;

public class GetClientContactDetailsQueryResponse : ResponseWithError
{
    public ClientContactDetailsResponse? Contact { get; set; }
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
    public required ClientContactListingDetailsResponse[] Listings { get; set; }
    public ClientContactAssociatedUsers[] AssociatedWith { get; set; } = [];
}

public class ClientContactListingDetailsResponse
{
    public long ListingInvitationId { get; set; }
    public long? ListingId { get; set; }
    public bool IsLeadAgent { get; set; }
    public ClientContactListingAgentDetailResponse[] Agents { get; set; } = [];
    public bool IsActive { get; set; }
    public required string Address { get; set; }
}

public class ClientContactListingAgentDetailResponse
{
    public long AgentId { get; set; }
    public required string Name { get; set; }
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
