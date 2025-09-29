using System.Reflection;
using RealtorApp.Domain.Interfaces;

namespace RealtorApp.Domain.Services;

public class SqlQueryService : ISqlQueryService
{
    public string GetChatQuery(string queryName)
    {
        var resourceName = $"RealtorApp.Domain.SqlQueries.Chat.{queryName}.sql";

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"SQL query resource not found: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}