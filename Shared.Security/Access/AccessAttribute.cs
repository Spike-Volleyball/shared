using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Shared.DataAccess.Providers.Interfaces;

namespace Shared.Security.Access;

/// <summary>
/// Declares the access a route needs to the resource it names, e.g.
/// <c>[Access&lt;EventAccess&gt;(EventAccess.Read, "eventId")]</c>. The resource's
/// <see cref="IResourceAuthority{TAccess}"/> decides before the action runs.
/// </summary>
/// <remarks>
/// A refusal is 404 for a signed-in caller, byte-identical to a missing resource, and 401 for a
/// signed-out one - the house rule that a refusal must not confirm the thing exists.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class AccessAttribute<TAccess>(TAccess access, string routeParameter)
    : Attribute, IFilterFactory, IOrderedFilter, IResourceAccessMetadata
    where TAccess : struct, Enum
{
    /// <summary>After the acting-as filter, which decides whose access is being asked about.</summary>
    private const int AfterSubjectResolution = 1000;

    public TAccess Access { get; } = access;

    public string RouteParameter { get; } = routeParameter;

    public int Order => AfterSubjectResolution;

    /// <summary>The authority is scoped, so the filter is made per request.</summary>
    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider) =>
        new ResourceAccessFilter<TAccess>(
            Access,
            RouteParameter,
            serviceProvider.GetRequiredService<IResourceAuthority<TAccess>>(),
            serviceProvider.GetRequiredService<IJwtPayloadProvider>());
}
