using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RealtorApp.Contracts.Queries.Listing.Responses;
using RealtorApp.Contracts.Queries.Responses;
using RealtorApp.Domain.Constants;
using RealtorApp.Domain.Interfaces;

namespace RealtorApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting(RateLimitConstants.Authenticated)]
    public class ListingsController(IListingService listingService, IUserAuthService userAuth) : RealtorApiBaseController
    {
        private readonly IListingService _listingService = listingService;
        private readonly IUserAuthService _userAuth = userAuth;

        [HttpGet("v1/{listingId}/slim")]
        public async Task<ActionResult<ListingDetailsSlimQueryResponse>> GetListingDetails([FromRoute] long listingId)
        {
            var isAssociatedToListing = await _userAuth.UserIsConnectedToListing(RequiredCurrentUserId, listingId);

            if (!isAssociatedToListing)
            {
                return BadRequest(new ListingDetailsSlimQueryResponse() { ClientNames = [], Address = "", ErrorMessage = "Request not allowed." });
            }

            var result = await _listingService.GetListingDetailsSlim(listingId);

            return Ok(result);
        }

        [HttpGet("v1/active")]
        public async Task<ActionResult<ActiveListingsQueryResponse>> GetActiveListings()
        {
            ActiveListingsQueryResponse response;

            if (CurrentUserRole == RoleConstants.Agent)
            {
                response = await _listingService.GetAgentActiveListings(RequiredCurrentUserId);
            } else
            {
                response = await _listingService.GetClientActiveListings(RequiredCurrentUserId);
            }

            return Ok(response);
        }
    }
}
