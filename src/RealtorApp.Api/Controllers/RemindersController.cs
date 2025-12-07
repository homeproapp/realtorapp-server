using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RealtorApp.Contracts.Commands.Reminders.Requests;
using RealtorApp.Contracts.Commands.Reminders.Responses;
using RealtorApp.Contracts.Queries.Reminders.Responses;
using RealtorApp.Domain.Constants;
using RealtorApp.Domain.Interfaces;

namespace RealtorApp.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
[EnableRateLimiting(RateLimitConstants.Authenticated)]
public class RemindersController(ILogger<RemindersController> logger, IReminderService reminderService, IUserAuthService userAuth) : RealtorApiBaseController
{
    private readonly ILogger<RemindersController> _logger = logger;
    private readonly IReminderService _reminderService = reminderService;
    private readonly IUserAuthService _userAuth = userAuth;

    [HttpPost("v1/reminder")]
    public async Task<ActionResult<AddOrUpdateReminderCommandResponse>> UpsertReminder([FromBody] AddOrUpdateReminderCommand command)
    {
        var isAllowed = await _userAuth.UserIsConnectedToListing(RequiredCurrentUserId, command.ListingId);
        if (!isAllowed) return BadRequest("Not allowed");

        var response = await _reminderService.AddOrUpdateReminder(command, RequiredCurrentUserId);
        return Ok(response);
    }

    [HttpGet("v1/{reminderId}")]
    public async Task<ActionResult<ReminderDetailsQueryResponse>> ReminderDetails([FromRoute] long reminderId)
    {
        var response = await _reminderService.GetReminderDetails(RequiredCurrentUserId, reminderId);
        var isAllowed = await _userAuth.UserIsConnectedToListing(RequiredCurrentUserId, response.ListingId);
        if (!isAllowed) return BadRequest("Not allowed");

        return Ok(response);
    }
}
