using Microsoft.AspNetCore.Authorization;

namespace Shared.Security.Endpoints;

/// <summary>
/// Served only on the service's internal listener, which the gateway never proxies to. Exempt
/// from the user fallback because its callers are other services; the host restriction added
/// alongside it is what keeps it off the public port.
/// </summary>
public sealed class InternalEndpointAttribute(int port) : Attribute, IAllowAnonymous
{
    public int Port { get; } = port;
}
