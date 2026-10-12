using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Shared.DTOs.Errors;

namespace Shared.Security.PublicReads;

/// <summary>What the limits on signed-out requests share: who the visitor is, and the answer that refuses them.</summary>
internal static class PublicVisitor
{
    /// <summary>The visitor's address as the gateway resolved it, overwriting whatever the client sent.</summary>
    public const string ClientAddressHeader = "X-Real-Client-Ip";

    /// <summary>Only a caller that reaches a service without the gateway, as a test host does, has no address.</summary>
    private const string NoAddress = "unknown";

    public static string AddressOf(HttpContext httpContext) =>
        (IPAddress.TryParse(httpContext.Request.Headers[ClientAddressHeader].ToString(), out var address)
            ? address
            : httpContext.Connection.RemoteIpAddress)?.ToString() ?? NoAddress;

    public static async ValueTask RejectAsync(OnRejectedContext context, CancellationToken cancellationToken)
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
