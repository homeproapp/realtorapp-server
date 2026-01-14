using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RealtorApp.Contracts.Queries.Search.Requests;
using RealtorApp.Contracts.Queries.Search.Responses;
using RealtorApp.Domain.Constants;
using RealtorApp.Domain.Interfaces;

namespace RealtorApp.Api.Controllers
{
    [Authorize(Policy = PolicyConstants.AgentOnly)] // may change in the future
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting(RateLimitConstants.Authenticated)]
    public class SearchController(ISearchService searchService) : RealtorApiBaseController
    {
        private readonly ISearchService _searchService = searchService;

        [HttpGet("v1/search/{entity}")]
        public async Task<ActionResult<SearchQueryResponse>> SearchEntity([FromQuery] SearchQuery query)
        {
            var result = await _searchService.GetEntitiesBasicSearch(query, RequiredCurrentUserId);
            return Ok(result);
        }
    }
}
