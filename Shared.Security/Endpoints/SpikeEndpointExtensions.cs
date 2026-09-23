using Grpc.AspNetCore.Server.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Shared.Security.Authorization;

namespace Shared.Security.Endpoints;

public static class SpikeEndpointExtensions
{
    private const string InternalListenerUrl = "Kestrel:Endpoints:Grpc:Url";

    /// <summary>Opens a mapped endpoint (health checks, minimal APIs) the way the attribute does.</summary>
    public static TBuilder AllowPublic<TBuilder>(this TBuilder builder, string reason)
        where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(new PublicEndpointAttribute(reason));

    /// <summary>Restricts an endpoint to the internal listener and marks it as internal.</summary>
    public static TBuilder RequireInternalListener<TBuilder>(this TBuilder builder, int port)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireHost($"*:{port}").WithMetadata(new InternalEndpointAttribute(port));

    /// <summary>
    /// A gRPC service reachable only on the internal listener. MapGrpcService alone also serves it
    /// on the public HTTP port, where only the gateways' HTTP/1.1 proxying kept it unreachable.
    /// </summary>
    public static GrpcServiceEndpointConventionBuilder MapInternalGrpcService<TService>(
        this IEndpointRouteBuilder endpoints, int port)
        where TService : class =>
        endpoints.MapGrpcService<TService>().RequireInternalListener(port);

    /// <summary>The port of the Kestrel endpoint named Grpc, the service's internal listener.</summary>
    public static int GetInternalListenerPort(this IConfiguration configuration)
    {
        var url = configuration[InternalListenerUrl]?.TrimEnd('/')
            ?? throw new InvalidOperationException($"{InternalListenerUrl} is not configured.");

        return int.TryParse(url[(url.LastIndexOf(':') + 1)..], out var port)
            ? port
            : throw new InvalidOperationException($"{InternalListenerUrl} names no port: {url}");
    }
}
