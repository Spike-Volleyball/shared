using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared.Security.Access;
using Shared.Security.Authorization;
using Shared.Security.Endpoints;

namespace Shared.Testing.Security;

/// <summary>
/// What a running service exposes, read from its endpoint metadata rather than its source, so class
/// and method attributes, conventions and mapped endpoints all count. Each method returns one line
/// per finding; a test asserts on the list.
/// </summary>
public static class EndpointSurface
{
    private const string ReasonSeparator = " - ";

    /// <summary>
    /// Every endpoint a signed-out caller reaches, with the reason it gives. Without a fallback
    /// policy that is every endpoint that declares nothing too, marked as such.
    /// </summary>
    public static IReadOnlyList<string> Public(IServiceProvider services)
    {
        var hasFallback = services.GetRequiredService<IOptions<AuthorizationOptions>>().Value.FallbackPolicy is not null;

        return Endpoints(services)
            .Where(e => e.Metadata.GetMetadata<InternalEndpointAttribute>() is null)
            .Select(e => e.Metadata.GetMetadata<IAllowAnonymous>() switch
            {
                PublicEndpointAttribute open => Describe(e) + ReasonSeparator + open.Reason,
                not null => Describe(e) + ReasonSeparator + "[AllowAnonymous] with no reason",
                null when !hasFallback && e.Metadata.GetOrderedMetadata<IAuthorizeData>().Count == 0 =>
                    Describe(e) + ReasonSeparator + "declares nothing, and there is no fallback policy",
                _ => null,
            })
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Endpoints served only on an internal listener, with the port.</summary>
    public static IReadOnlyList<string> Internal(IServiceProvider services) =>
        Endpoints(services)
            .Select(e => (Endpoint: e, Internal: e.Metadata.GetMetadata<InternalEndpointAttribute>()))
            .Where(x => x.Internal is not null)
            .Select(x => $"{Describe(x.Endpoint)}{ReasonSeparator}internal listener :{x.Internal!.Port}")
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>Endpoints opened with a bare [AllowAnonymous]: public, with nobody having said why.</summary>
    public static IReadOnlyList<string> BareAnonymous(IServiceProvider services) =>
        Endpoints(services)
            .Where(e => e.Metadata.GetMetadata<IAllowAnonymous>() is { } open
                        && open is not PublicEndpointAttribute and not InternalEndpointAttribute)
            .Select(Describe)
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// Reads a signed-out caller may make as often as they like: public, and under no rate limit. Public reads
    /// opt into the per-visitor limit, so what is left is the reviewed list of exceptions.
    /// </summary>
    public static IReadOnlyList<string> UnlimitedPublicReads(IServiceProvider services) =>
        Endpoints(services)
            .Where(e => e.Metadata.GetMetadata<InternalEndpointAttribute>() is null
                        && e.Metadata.GetMetadata<IAllowAnonymous>() is not null
                        && IsRead(e)
                        && (e.Metadata.GetMetadata<EnableRateLimitingAttribute>() is null
                            || e.Metadata.GetMetadata<DisableRateLimitingAttribute>() is not null))
            .Select(Describe)
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>Routes that name an id and declare neither [Access] nor [NoResourceScope].</summary>
    public static IReadOnlyList<string> UncheckedIdRoutes(IServiceProvider services) =>
        Endpoints(services)
            .Where(e => e.Metadata.GetMetadata<InternalEndpointAttribute>() is null)
            .Where(e => e.RoutePattern.Parameters.Any(p => IsId(p.Name)))
            .Where(e => e.Metadata.GetOrderedMetadata<IResourceAccessMetadata>().Count == 0
                        && e.Metadata.GetMetadata<NoResourceScopeAttribute>() is null)
            .Select(Describe)
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>[Access] declarations naming a route parameter their route does not have.</summary>
    public static IReadOnlyList<string> MisnamedAccess(IServiceProvider services) =>
        Endpoints(services)
            // Distinct: MVC lists a filter attribute twice, as an attribute and as a filter.
            .SelectMany(e => e.Metadata.GetOrderedMetadata<IResourceAccessMetadata>().Distinct()
                .Where(access => e.RoutePattern.GetParameter(access.RouteParameter) is null)
                .Select(access => $"{Describe(e)}{ReasonSeparator}no route parameter '{access.RouteParameter}'"))
            .Order(StringComparer.Ordinal)
            .ToList();

    private static IEnumerable<RouteEndpoint> Endpoints(IServiceProvider services) =>
        services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>();

    /// <summary>A GET, or a route that answers any method, as a mapped health check does.</summary>
    private static bool IsRead(RouteEndpoint endpoint) =>
        endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods is not { Count: > 0 } methods
        || methods.Contains(HttpMethods.Get);

    private static bool IsId(string parameter) =>
        parameter.Equals("id", StringComparison.OrdinalIgnoreCase)
        || parameter.EndsWith("Id", StringComparison.Ordinal);

    private static string Describe(RouteEndpoint endpoint)
    {
        var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods;
        var verb = methods is { Count: > 0 } ? string.Join(",", methods.Order(StringComparer.Ordinal)) : "ANY";
        return $"{verb} /{endpoint.RoutePattern.RawText?.TrimStart('/')}";
    }
}
