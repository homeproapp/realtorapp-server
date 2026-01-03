using RealtorApp.Contracts.Listings.Responses;
using RealtorApp.Contracts.Queries.Listing.Responses;
using RealtorApp.Contracts.Queries.Responses;

namespace RealtorApp.Domain.Interfaces;

public interface IListingService
{
    Task<ListingDetailsSlimQueryResponse> GetListingDetailsSlim(long listingId);
    Task<ActiveListingsQueryResponse> GetAgentActiveListings (long agentId);
    Task<ActiveListingsQueryResponse> GetClientActiveListings(long clientId);
    Task<DeleteListingCommandResponse> DeleteListing(long listingId, long agentId);
}
