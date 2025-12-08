using System;
using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Queries.Listing.Responses;

public class ListingDetailsSlimQueryResponse : ResponseWithError
{
    public required string[] ClientNames { get; set; }
    public required string[] AgentNames { get; set; }
    public required string Address { get; set; }
    public OtherListing[] OtherListings { get; set; } = [];
}

public class OtherListing
{
    public long ListingId { get; set; }
    public required string Address { get; set; }
}
