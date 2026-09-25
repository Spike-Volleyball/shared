using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.DTOs.Errors;
using Shared.Microservices.Authorization;

namespace Shared.Security.PublicReads;

/// <summary>
/// Signed-out reads of public endpoints, counted per visitor: <see cref="PublicReadSettings.PermitLimit"/> in a
/// window from one address, then 429 until the window turns. An action opts in with
/// <c>[EnableRateLimiting(PublicReadLimit.PolicyName)]</c>.
/// </summary>
/// <remarks>
/// Nearly every signed-out read arrives from the web's own server, rendering a page for whoever opened it, so
/// counting by address alone made the whole site one visitor: a crawler working through the sitemap would use up
/// the allowance and fail every uncached page (SPI-6469). The renderer proves itself with
/// <see cref="RendererHeader"/> and is not counted. Nor is anyone signed in: the app is known by its account,
/// and shares its address with everyone behind the same carrier or venue network.
/// </remarks>
public sealed class PublicReadLimit : IRateLimiterPolicy<string>
{
    public const string PolicyName = "public-reads";

    /// <summary>Carries <see cref="PublicReadSettings.RendererSecret"/> on the web server's reads.</summary>
    public const string RendererHeader = "X-Spike-Renderer";

    /// <summary>The visitor's address as the gateway resolved it, overwriting whatever the client sent.</summary>
    public const string ClientAddressHeader = "X-Real-Client-Ip";

    /// <summary>Only a caller that reaches a service without the gateway, as a test host does, has no address.</summary>
    private const string NoAddress = "unknown";

    /// <summary>The partition of every read that is not counted. No address reads like it.</summary>
    private const string NotCounted = "not-counted";

    private readonly PublicReadSettings _settings;

    public PublicReadLimit(IOptions<PublicReadSettings> settings, ILogger<PublicReadLimit> logger)
    {
        _settings = settings.Value;

        // Unset, nothing is limited: until the web's server sends the secret, its reads for every visitor
        // would be counted as one visitor. So this can deploy before a server has the secret, and a
        // development machine never needs one.
        if (string.IsNullOrEmpty(_settings.RendererSecret))
            logger.LogWarning(
                "{Section}:RendererSecret is not set, so signed-out reads of public endpoints are not rate-limited",
                PublicReadSettings.SectionName);
    }

    public RateLimitPartition<string> GetPartition(HttpContext httpContext) =>
        IsCounted(httpContext)
            ? RateLimitPartition.GetFixedWindowLimiter(VisitorOf(httpContext), _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = _settings.PermitLimit,
                Window = _settings.Window,
                QueueLimit = 0,
            })
            : RateLimitPartition.GetNoLimiter(NotCounted);

    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected { get; } = RejectAsync;

    private bool IsCounted(HttpContext httpContext) =>
        !string.IsNullOrEmpty(_settings.RendererSecret)
        && httpContext.User.Identity?.IsAuthenticated != true
        && !SharedSecret.Matches(_settings.RendererSecret, httpContext.Request.Headers[RendererHeader].ToString());

    private static string VisitorOf(HttpContext httpContext) =>
        (IPAddress.TryParse(httpContext.Request.Headers[ClientAddressHeader].ToString(), out var address)
            ? address
            : httpContext.Connection.RemoteIpAddress)?.ToString() ?? NoAddress;

    private static async ValueTask RejectAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var response = context.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);

        await response.WriteAsJsonAsync(
            ProblemDetailsResponse.TooManyRequests("Too many requests. Try again shortly."),
            options: null,
            contentType: "application/problem+json",
            cancellationToken);
    }
}
