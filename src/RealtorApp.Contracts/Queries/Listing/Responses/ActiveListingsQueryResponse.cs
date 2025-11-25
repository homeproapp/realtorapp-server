using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Queries.Responses;

public class ActiveListingsQueryResponse : ResponseWithError
{
    public ActiveListing[] ActiveListings { get; set; } = [];
}

public class ActiveListing
{
    public long ListingId { get; set; }
    public required string AddressLine1 { get; set; }
    public required string City { get; set; }
    public required string Region { get; set; }
}