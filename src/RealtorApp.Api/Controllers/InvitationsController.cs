using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using RealtorApp.Contracts.Commands.Invitations.Requests;
using RealtorApp.Contracts.Commands.Invitations.Responses;
using RealtorApp.Domain.Constants;
using RealtorApp.Domain.Interfaces;

namespace RealtorApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PolicyConstants.AgentOnly)]
public class InvitationsController(IInvitationService invitationService, ICryptoService crypto, IUserAuthService userAuthService) : RealtorApiBaseController
{
    private readonly IInvitationService _invitationService = invitationService;
    private readonly ICryptoService _crypto = crypto;
    private readonly IUserAuthService _userAuthService = userAuthService;

    [HttpPost("v1/send")]
    [EnableRateLimiting("Authenticated")]
    public async Task<ActionResult<SendInvitationCommandResponse>> SendInvitationsAsync([FromBody] SendInvitationCommand command)
    {
        var agentUserId = RequiredCurrentUserId;
        var response = await _invitationService.SendClientInvitationsAsync(command, agentUserId);

        if (response.Errors.Count > 0)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost("v1/send/teammate")]
    [EnableRateLimiting("Authenticated")]
    public async Task<ActionResult<SendTeammateInvitationCommandResponse>> SendTeammateInvitationsAsync([FromBody] SendTeammateInvitationCommand command)
    {
        var agentUserId = RequiredCurrentUserId;

        var isAssociatedToListing = await _userAuthService.UserIsConnectedToListing(agentUserId, command.ListingId);

        if (!isAssociatedToListing)
        {
            return BadRequest(new SendTeammateInvitationCommandResponse() { ErrorMessage = "Not allowed" });
        }

        var response = await _invitationService.SendTeammateInvitationsAsync(command, agentUserId);

        if (!string.IsNullOrEmpty(response.ErrorMessage))
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpGet("v1/validate")]
    [EnableRateLimiting("Anonymous")]
    public async Task<ActionResult<ValidateInvitationResponse>> ValidateInvitationAsync([FromQuery] string data)
    {
        var decryptedData = _crypto.Decrypt(data);
        var queryParams = QueryHelpers.ParseQuery(decryptedData);

        var validGuid = Guid.TryParse(queryParams[InvitationsConstants.InvitationTokenParam], out Guid inviteGuid);

        if (!validGuid)
        {
            return BadRequest(new ValidateInvitationResponse() { ErrorMessage = "Invalid invite data" });
        }

        var response = await _invitationService.ValidateClientInvitationAsync(inviteGuid);

        if (!response.IsValid)
        {
            return BadRequest(response);
        }

        _ = bool.TryParse(queryParams[InvitationsConstants.ExistingUserParam], out var isExistingUser);

        response.IsExistingUser = isExistingUser;

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpGet("v1/validate/teammate")]
    [EnableRateLimiting("Anonymous")]
    public async Task<ActionResult<ValidateTeammateInvitationResponse>> ValidateTeammateInvitationAsync([FromQuery] string data)
    {
        var decryptedData = _crypto.Decrypt(data);
        var queryParams = QueryHelpers.ParseQuery(decryptedData);

        var validGuid = Guid.TryParse(queryParams[InvitationsConstants.InvitationTokenParam], out Guid inviteGuid);

        if (!validGuid)
        {
            return BadRequest(new ValidateTeammateInvitationResponse() { ErrorMessage = "Invalid invite data" });
        }

        var response = await _invitationService.ValidateTeammateInvitationAsync(inviteGuid);

        if (!response.IsValid)
        {
            return BadRequest(response);
        }

        _ = bool.TryParse(queryParams[InvitationsConstants.ExistingUserParam], out var isExistingUser);

        response.IsExistingUser = isExistingUser;

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("v1/accept")]
    [EnableRateLimiting("Anonymous")]
    public async Task<ActionResult<AcceptInvitationCommandResponse>> AcceptInvitationAsync([FromBody] AcceptInvitationCommand command)
    {
        var response = await _invitationService.AcceptClientInvitationAsync(command);
        return Ok(response);
    }

    [HttpPost("v1/accept/token")]
    [Authorize(Policy = PolicyConstants.ClientOnly)]
    public async Task<ActionResult<AcceptInvitationWithTokenCommandResponse>> AcceptInvitationWithTokenAsync([FromBody] AcceptInvitationWithTokenCommand command)
    {
        var response = await _invitationService.AcceptClientInvitationWithTokenAsync(command, RequiredCurrentUserId);
        return Ok(response);
    }

    [HttpPost("v1/accept/teammate/token")]
    [Authorize(Policy = PolicyConstants.AgentOnly)]
    public async Task<ActionResult<AcceptInvitationWithTokenCommandResponse>> AcceptTeammateInvitationAsync([FromBody] AcceptInvitationWithTokenCommand command)
    {
        var response = await _invitationService.AcceptTeammateInvitationWithTokenAsync(command, RequiredCurrentUserId);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("v1/accept/teammate")]
    [EnableRateLimiting("Anonymous")]
    public async Task<ActionResult<AcceptInvitationCommandResponse>> AcceptTeammateInvitationAsync([FromBody] AcceptInvitationCommand command)
    {
        var response = await _invitationService.AcceptTeammateInvitationAsync(command);
        return Ok(response);
    }

    [HttpPut("v1/resend")]
    [EnableRateLimiting("Authenticated")]
    public async Task<ActionResult<ResendInvitationCommandResponse>> ResendInvitationAsync([FromBody] ResendInvitationCommand command)
    {
        var agentUserId = RequiredCurrentUserId;
        var response = await _invitationService.ResendClientInvitationAsync(command, agentUserId);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPut("v1/resend/teammate")]
    [EnableRateLimiting("Authenticated")]
    public async Task<ActionResult<ResendInvitationCommandResponse>> ResendTeammateInvitationAsync([FromBody] ResendTeammateInvitationCommand command)
    {
        var agentUserId = RequiredCurrentUserId;
        var response = await _invitationService.ResendTeammateInvitationAsync(command, agentUserId);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpDelete("v1/teammate/{teammateInvitationId}")]
    [EnableRateLimiting("Authenticated")]
    public async Task<ActionResult<RemoveTeammateInvitationResponse>> RemoveTeammateInvitationAsync([FromRoute] long teammateInvitationId)
    {
        var agentUserId = RequiredCurrentUserId;
        var response = await _invitationService.RemoveTeammateInvitationAsync(teammateInvitationId, agentUserId);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }
}
