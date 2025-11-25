using RealtorApp.Contracts.Queries.Listing.Responses;
using RealtorApp.Contracts.Queries.Responses;

namespace RealtorApp.Domain.Interfaces;

public interface IListingService
{
    Task<ListingDetailsSlimQueryResponse> GetListingDetailsSlim(long listingId);
    Task<ActiveListingsQueryResponse> GetActiveListings (long agentId);
}
