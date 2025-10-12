using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealtorApp.Contracts.Commands.Tasks.Requests;
using RealtorApp.Contracts.Commands.Tasks.Responses;
using RealtorApp.Contracts.Queries.Tasks.Requests;
using RealtorApp.Contracts.Queries.Tasks.Responses;
using RealtorApp.Domain.Interfaces;

namespace RealtorApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TasksController(ITaskService taskService, IUserAuthService userAuth) : RealtorApiBaseController
    {
        private readonly ITaskService _taskService = taskService;
        private readonly IUserAuthService _userAuth = userAuth;

        [HttpGet("v1/clients")]
        public async Task<ActionResult<ClientGroupedTasksListQueryResponse>> GetClients([FromQuery] ClientGroupedTasksListQuery query)
        {
            var clients = await _taskService.GetClientGroupedTasksListAsync(query, RequiredCurrentUserId);

            if (!string.IsNullOrEmpty(clients.ErrorMessage))
            {
                return BadRequest(clients);
            }

            return Ok(clients);
        }

        [HttpGet("v1/listings/{listingId}")]
        public async Task<ActionResult<SlimListingTasksQueryResponse>> GetListingTasks([FromRoute] long listingId, [FromQuery] ListingTasksQuery query)
        {
            var isAssociatedWithListing = await _userAuth.UserIsConnectedToListing(RequiredCurrentUserId, listingId);

            if (!isAssociatedWithListing)
            {
                return BadRequest(new SlimListingTasksQueryResponse() { ErrorMessage = "Not allowed" });
            }

            var tasks = await _taskService.GetListingTasksAsync(query, listingId);

            return Ok(tasks);
        }

        [HttpGet("v1/listings/{listingId}/slim")]
        public async Task<ActionResult<ListingTasksQueryResponse>> GetListingTasksSlim([FromRoute] long listingId, [FromQuery] ListingTasksQuery query)
        {
            var isAssociatedWithListing = await _userAuth.UserIsConnectedToListing(RequiredCurrentUserId, listingId);

            if (!isAssociatedWithListing)
            {
                return BadRequest(new ListingTasksQueryResponse() { ErrorMessage = "Not allowed" });
            }

            var tasks = await _taskService.GetSlimListingTasksAsync(listingId);

            return Ok(tasks);
        }

        [HttpPut("v1/listings/{listingId}")]
        public async Task<ActionResult<AddOrUpdateTaskCommandResponse>> UpdateTask([FromBody] AddOrUpdateTaskCommand command, [FromRoute] long listingId, [FromForm] IFormFile[]? images)
        {
            await Task.CompletedTask;
            return Ok();
        }
    }
}
