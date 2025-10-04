using RealtorApp.Contracts.Queries.Listing.Responses;

namespace RealtorApp.Domain.Interfaces;

public interface IListingService
{
    Task<ListingDetailsSlimQueryResponse> GetListingDetailsSlim(long listingId);
}
