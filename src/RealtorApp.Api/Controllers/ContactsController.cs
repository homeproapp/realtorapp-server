using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RealtorApp.Contracts.Commands.Contacts.Requests;
using RealtorApp.Contracts.Commands.Contacts.Responses;
using RealtorApp.Contracts.Queries.Contacts.Responses;
using RealtorApp.Domain.Constants;
using RealtorApp.Domain.Interfaces;

namespace RealtorApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting(RateLimitConstants.Authenticated)]
    public class ContactsController(IContactsService contactsService) : RealtorApiBaseController
    {
        private readonly IContactsService _contactsService = contactsService;

        [HttpGet("v1/third-parties")]
        public async Task<ActionResult<GetThirdPartyContactsQueryResponse>> GetThirdPartyContacts()
        {
            var result = await _contactsService.GetThirdPartyContactsAsync(RequiredCurrentUserId);
            return Ok(result);
        }

        [HttpGet("v1/third-parties/{id}")]
        public async Task<ActionResult<GetThirdPartyContactQueryResponse>> GetThirdPartyContact([FromRoute] long id)
        {
            var result = await _contactsService.GetThirdPartyContactAsync(id, RequiredCurrentUserId);

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("v1/third-parties")]
        public async Task<ActionResult<AddOrUpdateThirdPartyContactCommandResponse>> AddOrUpdateThirdPartyContact([FromBody] AddOrUpdateThirdPartyContactCommand command)
        {
            var result = await _contactsService.AddOrUpdateThirdPartyContactAsync(command, RequiredCurrentUserId);
            return Ok(result);
        }

        [HttpDelete("v1/third-parties/{id}")]
        public async Task<ActionResult<DeleteThirdPartyContactCommandResponse>> DeleteThirdPartyContact([FromRoute] long id)
        {
            var result = await _contactsService.DeleteThirdPartyContactAsync(id, RequiredCurrentUserId);

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
