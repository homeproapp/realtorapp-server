using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealtorApp.Contracts;
using RealtorApp.Contracts.Commands.Tasks.Requests;
using RealtorApp.Contracts.Commands.Tasks.Responses;
using RealtorApp.Contracts.Common.Requests;
using RealtorApp.Contracts.Queries.Tasks.Requests;
using RealtorApp.Contracts.Queries.Tasks.Responses;
using RealtorApp.Domain.Constants;
using RealtorApp.Domain.Extensions;
using RealtorApp.Domain.Interfaces;

namespace RealtorApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = PolicyConstants.AgentOnly)]
    public class TasksController(ITaskService taskService, IUserAuthService userAuth, ILogger<TasksController> logger, IReminderService reminderService) : RealtorApiBaseController
    {
        private readonly ITaskService _taskService = taskService;
        private readonly IUserAuthService _userAuth = userAuth;
        private readonly ILogger<TasksController> _logger = logger;
        private readonly IReminderService _reminderService = reminderService;

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
        [Authorize(Policy = PolicyConstants.ClientOrAgent)]

        public async Task<ActionResult<ListingTasksQueryResponse>> GetListingTasks([FromRoute] long listingId, [FromQuery] ListingTasksQuery query)
        {
            var isAssociatedWithListing = await _userAuth.UserIsConnectedToListing(RequiredCurrentUserId, listingId);

            if (!isAssociatedWithListing)
            {
                return BadRequest(new ListingTasksQueryResponse() { ErrorMessage = "Not allowed" });
            }

            var tasks = await _taskService.GetListingTasksAsync(query, listingId);
            long[] taskIds = [.. tasks.Keys];
            var taskReminders = await _reminderService.GetUsersTaskReminders(RequiredCurrentUserId, taskIds);

            foreach (var taskReminder in taskReminders)
            {
                if (tasks.TryGetValue(taskReminder.ReferencingObjectId, out TaskListItemResponse? task) && task != null)
                {
                    task.TaskReminders.Add(new TaskReminderSlim()
                    {
                        ReminderId = taskReminder.ReminderId,
                        RemindAt = taskReminder.RemindAt,
                        ReminderText = taskReminder.ReminderText
                    });
                }
            }

            TaskListItemResponse[] tasksArray =  [.. tasks.Values];

            var response = new ListingTasksQueryResponse()
            {
                Tasks = tasksArray,
                TaskCompletionCounts = tasksArray.ToCompletionCounts(),
                FilterOptions = tasksArray.ToFilterOptions(),
            };

            return Ok(response);
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

        [HttpPost("v1/listings/{listingId}")]
        public async Task<ActionResult<AddOrUpdateTaskCommandResponse>> UpsertTask([FromForm] string commandJson, [FromRoute] long listingId, [FromForm] IFormFile[] newImages)
        {
            var isAssociatedWithListing = await _userAuth.UserIsConnectedToListing(RequiredCurrentUserId, listingId);

            if (!isAssociatedWithListing)
            {
                return BadRequest(new AddOrUpdateTaskCommandResponse() { ErrorMessage = "Not allowed" });
            }

            AddOrUpdateTaskCommand? command;
            try
            {
                var options = new JsonSerializerOptions()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };
                command = JsonSerializer.Deserialize<AddOrUpdateTaskCommand>(commandJson, options);
                if (command == null)
                {
                    return BadRequest(new AddOrUpdateTaskCommandResponse() { ErrorMessage = "Invalid request" });
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Unable to parse command json");
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

            if (newImages != null && newImages.Length > 0)
            {
                var imageValidator = new Validators.TaskImagesValidator();
                var imageValidationResult = await imageValidator.ValidateAsync(newImages);
                if (!imageValidationResult.IsValid)
                {
                    return BadRequest(new AddOrUpdateTaskCommandResponse()
                    {
                        ErrorMessage = string.Join("; ", imageValidationResult.Errors.Select(e => e.ErrorMessage))
                    });
                }

                newFiles = [.. newImages.Select(file => new FileUploadRequest
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

        [HttpDelete("v1/{listingId}/{taskId}")]
        [Authorize(Policy = PolicyConstants.ClientOrAgent)]
        public async Task<ActionResult> DeleteTask([FromRoute] long listingId, [FromRoute] long taskId)
        {
            var isAssociatedWithListing = await _userAuth.UserIsConnectedToListing(RequiredCurrentUserId, listingId);

            if (!isAssociatedWithListing)
            {
                return BadRequest(new { ErrorMessage = "Not allowed" });
            }

            var deleted = await _taskService.MarkTaskAndChildrenAsDeleted(taskId, listingId);

            if (!deleted)
            {
                return NotFound(new { ErrorMessage = "Task not found" });
            }

            return Ok(new { Message = "Task deleted successfully" });
        }

        [HttpPatch("v1/{listingId}/{taskId}")]
        [Authorize(Policy = PolicyConstants.ClientOnly)]
        public async Task<ActionResult> UpdateTaskStatus([FromRoute] long listingId, [FromRoute] long taskId, [FromBody] UpdateTaskStatusCommand command)
        {
            var isAssociatedWithListing = await _userAuth.UserIsConnectedToListing(RequiredCurrentUserId, listingId);

            if (!isAssociatedWithListing)
            {
                return BadRequest(new { ErrorMessage = "Not allowed" });
            }

            var rowsUpdated = await _taskService.UpdateTaskStatusAsync(taskId, listingId, command.NewStatus);

            if (rowsUpdated == 0)
            {
                _logger.LogWarning("UpdateTaskStatus updated 0 rows for listing {listingId} and task {taskId}", listingId, taskId);
                return BadRequest("Unable to update task status");
            }

            return Ok(new { Message = "Task status updated" });
        }
    }
}
