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
    [EnableRateLimiting(RateLimitConstants.Authenticated)]
    public class ContactsController(IContactsService contactsService, IUserAuthService userAuthService) : RealtorApiBaseController
    {
        private readonly IContactsService _contactsService = contactsService;
        private readonly IUserAuthService _userAuthService = userAuthService;

        [Authorize(Policy = PolicyConstants.AgentOnly)]
        [HttpGet("v1/third-parties")]
        public async Task<ActionResult<GetThirdPartyContactsQueryResponse>> GetThirdPartyContacts()
        {
            var result = await _contactsService.GetThirdPartyContactsAsync(RequiredCurrentUserId);
            return Ok(result);
        }

        [Authorize(Policy = PolicyConstants.AgentOnly)]
        [HttpGet("v1/clients")]
        public async Task<ActionResult<GetClientContactsSlimQueryResponse>> GetClientContacts()
        {
            var result = await _contactsService.GetClientContactsSlimAsync(RequiredCurrentUserId);
            return Ok(result);
        }

        [Authorize(Policy = PolicyConstants.AgentOnly)]
        [HttpGet("v1/clients/{contactId}")]
        public async Task<ActionResult<GetClientContactDetailsQueryResponse>> GetClientContactDetails(long contactId)
        {
            var result = await _contactsService.GetClientContactDetailsAsync(contactId, RequiredCurrentUserId);

            return Ok(result);
        }

        [Authorize(Policy = PolicyConstants.AgentOnly)]
        [HttpDelete("v1/clients/{contactId}")]
        public async Task<ActionResult<DeleteClientContactCommandResponse>> DeleteClientContactDetails(long contactId)
        {
            var result = await _contactsService.DeleteClientContact(contactId, RequiredCurrentUserId);

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [Authorize(Policy = PolicyConstants.AgentOnly)]
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

        [Authorize(Policy = PolicyConstants.AgentOnly)]
        [HttpPost("v1/third-parties")]
        public async Task<ActionResult<AddOrUpdateThirdPartyContactCommandResponse>> AddOrUpdateThirdPartyContact([FromBody] AddOrUpdateThirdPartyContactCommand command)
        {
            var result = await _contactsService.AddOrUpdateThirdPartyContactAsync(command, RequiredCurrentUserId);
            return Ok(result);
        }

        [Authorize(Policy = PolicyConstants.AgentOnly)]
        [HttpPost("v1/third-parties/bulk")]
        public async Task<ActionResult<GetThirdPartyContactsQueryResponse>> BulkAddThirdPartyContacts([FromBody] BulkAddThirdPartyContactCommand command)
        {
            var result = await _contactsService.BulkAddThirdPartyContactAsync(command, RequiredCurrentUserId);
            return Ok(result);
        }

        [Authorize(Policy = PolicyConstants.AgentOnly)]
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

        [HttpGet("v1/conversations/{conversationId}/attachments/{attachmentId}/contact")]
        [Authorize(Policy = PolicyConstants.ClientOrAgent)]
        public async Task<ActionResult<GetThirdPartyContactQueryResponse>> GetThirdPartyContactByAttachment(
            [FromRoute] long conversationId,
            [FromRoute] long attachmentId)
        {
            var isAllowed = await _userAuthService.IsConversationParticipant(RequiredCurrentUserId, conversationId);

            if (!isAllowed)
            {
                return Unauthorized("Not authorized to access this conversation");
            }

            var result = await _contactsService.GetThirdPartyContactByAttachmentAsync(attachmentId);

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
