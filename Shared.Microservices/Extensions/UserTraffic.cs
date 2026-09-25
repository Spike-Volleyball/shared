using Microsoft.AspNetCore.Http;

namespace Shared.Microservices.Extensions;

/// <summary>
/// The requests traces and the HTTP request histogram describe: the ones people made. Health
/// probes and Prometheus scrapes are most of a quiet service's requests, and a SignalR hub
/// connection is one request that lives for minutes (its frames are not requests at all), so
/// all three are left out of both — and a service with no users shows no traffic.
/// </summary>
internal static class UserTraffic
{
    public static bool Includes(HttpContext context) =>
        !context.Request.Path.StartsWithSegments("/health") &&
        !context.Request.Path.StartsWithSegments("/metrics") &&
        !context.Request.Path.StartsWithSegments("/hubs");
}
