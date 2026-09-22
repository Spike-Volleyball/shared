using Microsoft.AspNetCore.Authorization;

namespace Shared.Security.Authorization;

/// <summary>
/// The one way to open an endpoint to signed-out callers under deny-by-default. The reason is
/// part of the service's reviewed public surface, and a bare <see cref="AllowAnonymousAttribute"/>
/// fails the architecture test, so an endpoint cannot go public without someone saying why.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class PublicEndpointAttribute : AllowAnonymousAttribute
{
    public PublicEndpointAttribute(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Say why this endpoint is public.", nameof(reason));

        Reason = reason;
    }

    public string Reason { get; }
}
