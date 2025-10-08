using Microsoft.AspNetCore.Http;

namespace RealtorApp.Api.Extensions;

public static class HttpResponseExtensions
{
    public static void SetImageCacheHeaders(this HttpResponse response, int cacheDurationInSeconds)
    {
        response.Headers.CacheControl = $"public, max-age={cacheDurationInSeconds}";
        response.Headers.Expires = DateTime.UtcNow.AddSeconds(cacheDurationInSeconds).ToString("R");
    }
}
