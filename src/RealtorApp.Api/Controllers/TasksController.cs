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
    public class TasksController(ITaskService taskService, IUserAuthService userAuth, ILogger<TasksController> logger, IReminderService reminderService) : RealtorApiBaseController
    {
        private readonly ITaskService _taskService = taskService;
        private readonly IUserAuthService _userAuth = userAuth;
        private readonly ILogger<TasksController> _logger = logger;
        private readonly IReminderService _reminderService = reminderService;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        [HttpGet("v1/listings")]
        [Authorize(Policy = PolicyConstants.AgentOnly)]
        public async Task<ActionResult<ListingTasksListDetailsQueryResponse>> GetListings([FromQuery] ListingsTaskListQuery query)
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

        [HttpGet("v1/listings/{listingId}/bulk")]
        [Authorize(Policy = PolicyConstants.AgentOnly)]
        public async Task<ActionResult<ListingTasksQueryResponse>> BulkGetTasks([FromRoute] long listingId, [FromQuery] long[] taskIds)
        {
            var isAssociatedWithListing = await _userAuth.UserIsConnectedToListing(RequiredCurrentUserId, listingId);

            if (!isAssociatedWithListing)
            {
                return BadRequest(new ListingTasksQueryResponse() { ErrorMessage = "Not allowed" });
            }

            var tasks = await _taskService.BulkGetTasksByIds(taskIds, listingId);
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
        [Authorize(Policy = PolicyConstants.ClientOrAgent)]
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

        [HttpGet("v1/{listingId}/{taskId}")]
        [Authorize(Policy = PolicyConstants.ClientOrAgent)]
        public async Task<ActionResult<TaskListItemResponse>> GetTask([FromRoute] long listingId, [FromRoute] long taskId)
        {
            var isAssociatedWithListing = await _userAuth.UserIsConnectedToListing(RequiredCurrentUserId, listingId);

            if (!isAssociatedWithListing)
            {
                return BadRequest(new { ErrorMessage = "Not allowed" });
            }

            var task = await _taskService.GetTaskByIdAsync(taskId, listingId);

            if (task == null)
            {
                return NotFound(new { ErrorMessage = "Task not found" });
            }

            var taskReminders = await _reminderService.GetUsersTaskReminders(RequiredCurrentUserId, [taskId]);
            task.TaskReminders.AddRange(taskReminders.Select(tr => new TaskReminderSlim()
            {
                ReminderId = tr.ReminderId,
                RemindAt = tr.RemindAt,
                ReminderText = tr.ReminderText
            }));

            return Ok(task);
        }

        [HttpPost("v1/listings/{listingId}")]
        [Authorize(Policy = PolicyConstants.AgentOnly)]
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

                command = JsonSerializer.Deserialize<AddOrUpdateTaskCommand>(commandJson, _jsonOptions);
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

            try
            {
                var updatedTask = await _taskService.AddOrUpdateTaskAsync(command, listingId, newFiles);
                return Ok(updatedTask);
            }
            finally
            {
                foreach (var file in newFiles)
                {
                    file.Dispose();
                }
            }
        }

        [HttpPost("v1/{listingId}/ai-create")]
        [Authorize(Policy = PolicyConstants.AgentOnly)]
        public async Task<ActionResult<AiTaskCreateCommandResponse>> AiCreateTasks([FromForm] IFormFile[] images, [FromForm] string metadata, [FromForm] IFormFile audio, [FromRoute] long listingId)
        {
            if (audio == null || audio.Length == 0)
            {
                return BadRequest("No recording detected");
            }

            AiTaskCreateMetadataCommand[] parsedMetadata = [];
            try
            {
                parsedMetadata = JsonSerializer.Deserialize<AiTaskCreateMetadataCommand[]>(metadata, _jsonOptions) ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something went wrong parsing metadata");
            }

            if (parsedMetadata.Length == 0 && parsedMetadata.Any(i => string.IsNullOrEmpty(i.FileName) || string.IsNullOrEmpty(i.Timestamp)))
            {
                _logger.LogError("Parsed metadata was invalid, original string = {Metadata}", metadata);
                return BadRequest("Something went wrong processing request");
            }

            if (images.Length != parsedMetadata.Length)
            {
                _logger.LogError("Difference in counts between metadata and uploaded images");
                return BadRequest("Something went wrong processing request");
            }

            var audioUploadRequest = new FileUploadRequest()
            {
                FileExtension = Path.GetExtension(audio.FileName),
                FileName = Path.GetFileName(audio.FileName),
                Content = audio.OpenReadStream(),
                ContentType = audio.ContentType,
                ContentLength = audio.Length
            };

            FileUploadRequest[] imageUploadRequests = [.. images.Select(file => new FileUploadRequest()
            {
                    Content = file.OpenReadStream(),
                    FileName = Path.GetFileName(file.FileName),
                    FileExtension = Path.GetExtension(file.FileName),
                    ContentType = file.ContentType,
                    ContentLength = file.Length
            })];

            var response = await _taskService.AiCreateTasks(audioUploadRequest, imageUploadRequests, parsedMetadata, listingId);

            if (response == null || response.Length == 0)
            {
                return BadRequest(new AiTaskCreateCommandResponse() { ErrorMessage = "Unable to create tasks" });
            }

            return Ok(new AiTaskCreateCommandResponse()
            {
                TaskIds = response
            });
        }

        [HttpDelete("v1/{listingId}")]
        [Authorize(Policy = PolicyConstants.AgentOnly)]
        public async Task<ActionResult> BulkDeleteTasks([FromRoute] long listingId, [FromBody] long[] taskIds)
        {
            var isAssociatedWithListing = await _userAuth.UserIsConnectedToListing(RequiredCurrentUserId, listingId);

            if (!isAssociatedWithListing)
            {
                return BadRequest(new { ErrorMessage = "Not allowed" });
            }

            var deleted = await _taskService.BulkMarkTaskAndChildrenAsDeleted(taskIds, listingId);

            if (!deleted)
            {
                return NotFound(new { ErrorMessage = "Unable to delete tasks" });
            }

            return Ok(new { Message = "Tasks deleted successfully" });
        }


        [HttpDelete("v1/{listingId}/{taskId}")]
        [Authorize(Policy = PolicyConstants.AgentOnly)]
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
