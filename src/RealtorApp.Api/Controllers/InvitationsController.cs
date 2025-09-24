using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RealtorApp.Contracts.Commands.Invitations;
using RealtorApp.Domain.Interfaces;

namespace RealtorApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvitationsController(IInvitationService invitationService) : BaseController
{
    private readonly IInvitationService _invitationService = invitationService;

    [HttpPost("send")]
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
    [HttpGet("{token}")]
    [EnableRateLimiting("Anonymous")]
    public async Task<ActionResult<ValidateInvitationResponse>> ValidateInvitationAsync([FromRoute] Guid token)
    {
        var response = await _invitationService.ValidateInvitationAsync(token);

        if (!response.IsValid)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("accept")]
    [EnableRateLimiting("Anonymous")]
    public async Task<ActionResult<AcceptInvitationCommandResponse>> AcceptInvitationAsync([FromBody] AcceptInvitationCommand command)
    {
        var response = await _invitationService.AcceptInvitationAsync(command);
        return Ok(response);
    }
}