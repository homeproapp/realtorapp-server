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
    public class TasksController(ITaskService taskService) : RealtorApiBaseController
    {
        private readonly ITaskService _taskService = taskService;

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
        public async Task<ActionResult<ListingTasksQueryResponse[]>> GetListingTasks([FromRoute] long listingId, [FromQuery] ListingTasksQuery query)
        {
            var tasks = await _taskService.GetListingTasksAsync(query, listingId);

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
