using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Queries.Listing.Responses;


public class ListingsQueryResponse : ResponseWithError
{
    public ListingItem[] Listings { get; set; } = [];
    public bool HasMore { get; set; }
}

public class ListingItem
{
    public long? ListingId { get; set; }
    public required string AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public required string Status { get; set; }
    public bool IsSupportingOnListing { get; set; }
    //TODO: will need to add a field for every type of support staff
    public ListingAgent[] ListingAgents { get; set; } = [];
    public PendingTeammateInvitation[] InvitedTeammates { get; set; } = [];
}

public class PendingTeammateInvitation
{
    public long TeammateInvitationId { get; set; }
    public required string Email { get; set; }
    public bool IsExpired { get; set; }
}

public class ListingAgent
{
    public long AgentId { get; set; }
    public required string Name { get; set; }
}
