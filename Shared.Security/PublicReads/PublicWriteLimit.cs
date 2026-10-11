using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Shared.Security.PublicReads;

/// <summary>
/// Signed-out writes to public endpoints, counted per visitor: <see cref="PublicWriteSettings.PermitLimit"/> in a
/// window from one address, then 429 until the window turns. An action opts in with
/// <c>[EnableRateLimiting(PublicWriteLimit.PolicyName)]</c>.
/// </summary>
/// <remarks>
/// Stricter than <see cref="PublicReadLimit"/>, and without its exemptions: the web's server renders pages for
/// visitors but does not submit forms for them, and a write costs somebody attention whoever is signed in.
/// </remarks>
public sealed class PublicWriteLimit : IRateLimiterPolicy<string>
{
    public const string PolicyName = "public-writes";

    private readonly PublicWriteSettings _settings;

    public PublicWriteLimit(IOptions<PublicWriteSettings> settings)
    {
        _settings = settings.Value;
    }

    public RateLimitPartition<string> GetPartition(HttpContext httpContext) =>
        RateLimitPartition.GetFixedWindowLimiter(PublicVisitor.AddressOf(httpContext), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = _settings.PermitLimit,
            Window = _settings.Window,
            QueueLimit = 0,
        });

    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected { get; } = PublicVisitor.RejectAsync;
}
