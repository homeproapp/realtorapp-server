using RealtorApp.Contracts.Commands.Tasks.Responses;
using RealtorApp.Domain.Models;
using Task = RealtorApp.Domain.Models.Task;

namespace RealtorApp.Domain.Extensions;

public static class TaskExtensions
{
    public static AddOrUpdateTaskCommandResponse FromNewTaskToTaskCommandResponse(this Task task)
    {
        return new AddOrUpdateTaskCommandResponse
        {
            TaskId = task.TaskId,
            AddedLinks = task.Links?.Select(l => new AddedLinkResponse
            {
                LinkId = l.LinkId,
                LinkText = l.Name,
                LinkUrl = l.Url
            }).ToArray()
        };
    }


    public static AddOrUpdateTaskCommandResponse FromExistingTaskToTaskCommandResponse(this Task task, List<Link> addedLinks)
    {
        return new AddOrUpdateTaskCommandResponse
        {
            TaskId = task.TaskId,
            AddedLinks = [.. addedLinks.Select(l => new AddedLinkResponse
            {
                LinkId = l.LinkId,
                LinkText = l.Name,
                LinkUrl = l.Url
            })]
        };
    }
}
