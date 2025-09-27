using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using RealtorApp.Contracts.Commands.Invitations;
using RealtorApp.Domain.Constants;
using RealtorApp.Domain.Interfaces;

namespace RealtorApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvitationsController(IInvitationService invitationService, ICryptoService crypto) : BaseController
{
    private readonly IInvitationService _invitationService = invitationService;
    private readonly ICryptoService _crypto = crypto;

    [HttpPost("v1/send")]
    [EnableRateLimiting("Authenticated")]
    public async Task<ActionResult<SendInvitationCommandResponse>> SendInvitationsAsync([FromBody] SendInvitationCommand command)
    {
        var agentUserId = RequiredCurrentUserId;
        var response = await _invitationService.SendInvitationsAsync(command, agentUserId);

        if (response.Errors.Count > 0)
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

        var response = await _invitationService.ValidateInvitationAsync(inviteGuid);

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
        var response = await _invitationService.AcceptInvitationAsync(command);
        return Ok(response);
    }

    [HttpPut("v1/resend")]
    [EnableRateLimiting("Authenticated")]
    public async Task<ActionResult<ResendInvitationCommandResponse>> ResendInvitationAsync([FromBody] ResendInvitationCommand command)
    {
        var agentUserId = RequiredCurrentUserId;
        var response = await _invitationService.ResendInvitationAsync(command, agentUserId);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }
}