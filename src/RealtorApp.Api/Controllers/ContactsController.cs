using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RealtorApp.Contracts.Queries.Contacts.Responses;
using RealtorApp.Domain.Constants;

namespace RealtorApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting(RateLimitConstants.Authenticated)]
    public class ContactsController : ControllerBase
    {

        [HttpGet("v1/third-parties")]
        public async Task<ActionResult<GetThirdPartyContactsQueryResponse>> GetThirdPartyContacts()
        {
            await Task.CompletedTask;
            return Ok();
        }

        [HttpGet("v1/third-parties/{id}")]
        public async Task<ActionResult<GetThirdPartyContactQueryResponse>> GetThirdPartyContact([FromRoute] long id)
        {
            await Task.CompletedTask;
            return Ok();
        }
    }
}
