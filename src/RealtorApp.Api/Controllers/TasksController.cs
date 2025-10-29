using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealtorApp.Contracts.Commands.Tasks.Requests;
using RealtorApp.Contracts.Commands.Tasks.Responses;
using RealtorApp.Contracts.Common.Requests;
using RealtorApp.Contracts.Queries.Tasks.Requests;
using RealtorApp.Contracts.Queries.Tasks.Responses;
using RealtorApp.Domain.Extensions;
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
        public async Task<ActionResult<ListingTasksQueryResponse>> GetListingTasks([FromRoute] long listingId, [FromQuery] ListingTasksQuery query)
        {
            var isAssociatedWithListing = await _userAuth.UserIsConnectedToListing(RequiredCurrentUserId, listingId);

            if (!isAssociatedWithListing)
            {
                return BadRequest(new ListingTasksQueryResponse() { ErrorMessage = "Not allowed" });
            }

            var tasks = await _taskService.GetListingTasksAsync(query, listingId);

            return Ok(tasks);
        }

        [HttpGet("v1/listings/{listingId}/slim")]
        public async Task<ActionResult<SlimListingTasksQueryResponse>> GetListingTasksSlim([FromRoute] long listingId, [FromQuery] ListingTasksQuery query)
        {
            var isAssociatedWithListing = await _userAuth.UserIsConnectedToListing(RequiredCurrentUserId, listingId);

            if (!isAssociatedWithListing)
            {
                return BadRequest(new SlimListingTasksQueryResponse() { ErrorMessage = "Not allowed" });
            }

            var tasks = await _taskService.GetSlimListingTasksAsync(listingId);

            return Ok(tasks);
        }

        [HttpPost("v1/listings/{listingId}/task")]
        public async Task<ActionResult<AddOrUpdateTaskCommandResponse>> UpsertTask([FromForm] string commandJson, [FromRoute] long listingId, [FromForm] IFormFile[]? images)
        {
            var isAssociatedWithListing = await _userAuth.UserIsConnectedToListing(RequiredCurrentUserId, listingId);

            if (!isAssociatedWithListing)
            {
                return BadRequest(new AddOrUpdateTaskCommandResponse() { ErrorMessage = "Not allowed" });
            }

            AddOrUpdateTaskCommand? command;
            try
            {
                command = JsonSerializer.Deserialize<AddOrUpdateTaskCommand>(commandJson);
                if (command == null)
                {
                    return BadRequest(new AddOrUpdateTaskCommandResponse() { ErrorMessage = "Invalid request" });
                }
            }
            catch (JsonException)
            {
                return BadRequest(new AddOrUpdateTaskCommandResponse() { ErrorMessage = "Invalid request" });
            }

            var commandValidator = new Validators.AddOrUpdateTaskCommandValidator();
            var commandValidationResult = await commandValidator.ValidateAsync(command);
            if (!commandValidationResult.IsValid)
            {
                return BadRequest(new AddOrUpdateTaskCommandResponse()
                {
                    ErrorMessage = string.Join("; ", commandValidationResult.Errors.Select(e => e.ErrorMessage))
                });
            }

            FileUploadRequest[] newFiles = [];

            if (images != null && images.Length > 0)
            {
                var imageValidator = new Validators.TaskImagesValidator();
                var imageValidationResult = await imageValidator.ValidateAsync(images);
                if (!imageValidationResult.IsValid)
                {
                    return BadRequest(new AddOrUpdateTaskCommandResponse()
                    {
                        ErrorMessage = string.Join("; ", imageValidationResult.Errors.Select(e => e.ErrorMessage))
                    });
                }

                newFiles = [.. images.Select(file => new FileUploadRequest
                {
                    Content = file.OpenReadStream(),
                    FileName = Path.GetFileName(file.FileName),
                    FileExtension = Path.GetExtension(file.FileName),
                    ContentType = file.ContentType,
                    ContentLength = file.Length
                })];
            }

            var updatedTask = await _taskService.AddOrUpdateTaskAsync(command, listingId, newFiles);

            return Ok(updatedTask);
        }
    }
}
