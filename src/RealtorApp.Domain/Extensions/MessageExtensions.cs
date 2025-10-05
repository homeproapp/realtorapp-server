using RealtorApp.Contracts.Queries.Chat.Responses;

namespace RealtorApp.Domain.Extensions;

public static class MessageExtensions
{
    public static List<MessageGroupByDate> GroupMessagesByDate(this IEnumerable<MessageResponse> messages)
    {
        return messages
            .GroupBy(m => m.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new MessageGroupByDate
            {
                Date = g.Key,
                Messages = g.OrderBy(m => m.CreatedAt).ToList()
            })
            .ToList();
    }
}
