using Microsoft.AspNetCore.SignalR;

namespace RealtorApp.Api.Providers;

public class SubClaimUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst("sub")?.Value;
    }
}
