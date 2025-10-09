using RealtorApp.Contracts.Commands.Tasks.Responses;
using RealtorApp.Contracts.Enums;
using RealtorApp.Contracts.Queries.Tasks.Responses;
using RealtorApp.Domain.Models;
using Task = RealtorApp.Domain.Models.Task;
using TaskStatus = RealtorApp.Contracts.Enums.TaskStatus;

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

    public static TaskCompletionCountItem[] ToCompletionCounts(this TaskListItemResponse[] tasks)
    {
        var roomGroupings = tasks.GroupBy(i => i.Room);
        var priorityGroupings = tasks.GroupBy(i => i.Priority);
        var result = new List<TaskCompletionCountItem>(roomGroupings.Count() + priorityGroupings.Count() + 1);

        var maxCount = Math.Max(roomGroupings.Count(), priorityGroupings.Count());

        for (int i = 0; i < maxCount; i++)
        {
            var roomGroup = roomGroupings.ElementAtOrDefault(i);
            var priorityGroup = priorityGroupings.ElementAtOrDefault(i);

            if (roomGroup != null)
            {
                result.Add(new()
                {
                    Type = TaskCountType.Room,
                    Name = (roomGroup.Key ?? string.Empty) + " Tasks",
                    Completion = (double)roomGroup.Where(i => i.Status == (short)TaskStatus.Completed).Count() / roomGroup.Count()
                });
            }

            if (priorityGroup != null)
            {
                result.Add(new()
                {
                    Type = TaskCountType.Priority,
                    Name = ((TaskPriority)(priorityGroup.Key ?? 0)).ToString() + " Priority Tasks",
                    Completion = (double)priorityGroup.Where(i => i.Status == (short)TaskStatus.Completed).Count() / priorityGroup.Count()
                });
            }
        }

        result.Add(new()
        {
            Type = TaskCountType.Total,
            Name = "Total Completion",
            Completion = (double)tasks.Where(i => i.Status == (short)TaskStatus.Completed).Count() / tasks.Length
        });

        return [.. result];
    }
}
