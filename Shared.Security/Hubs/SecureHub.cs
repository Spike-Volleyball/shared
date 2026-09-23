using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Shared.DataAccess.Providers.Interfaces;
using Shared.Microservices.Authorization;
using Shared.Security.Access;

namespace Shared.Security.Hubs;

/// <summary>
/// The base for hubs under deny-by-default. Joining a resource's group passes the authority a route
/// to that resource passes, and the raw group manager is out of reach: a derived hub that touches
/// <see cref="Groups"/> does not compile. Hubs used to join anyone to any event's room (SPI-6441).
/// </summary>
/// <param name="services">The invocation's scope, which a hub is activated in; the connection's
/// HttpContext scope outlives it and would share one DbContext across invocations.</param>
public abstract class SecureHub(IServiceProvider services) : Hub
{
    [Obsolete("Join through JoinResourceGroupAsync, which asks the resource's authority first.", error: true)]
    protected new IGroupManager Groups => base.Groups;

    /// <summary>Adds the caller to <paramref name="group"/> once the resource's authority allows it.</summary>
    /// <exception cref="HubException">The same refusal whether the resource is hidden or missing.</exception>
    protected async Task JoinResourceGroupAsync<TAccess>(string group, Guid resourceId, TAccess access)
        where TAccess : struct, Enum
    {
        await EnsureAccessAsync(resourceId, access);
        await base.Groups.AddToGroupAsync(Context.ConnectionId, group, Context.ConnectionAborted);
    }

    /// <summary>
    /// Asks the resource's authority what a route declaring [Access] asks, for a hub method that acts
    /// on a resource by id: a hub method is another door to the same resource.
    /// </summary>
    /// <exception cref="HubException">The same refusal whether the resource is hidden or missing.</exception>
    protected async Task EnsureAccessAsync<TAccess>(Guid resourceId, TAccess access)
        where TAccess : struct, Enum
    {
        var authority = services.GetRequiredService<IResourceAuthority<TAccess>>();
        var userId = CallerIdentity.UserId(Context.User ?? new(), services.GetRequiredService<IJwtPayloadProvider>());

        if (!await authority.CanAsync(userId, resourceId, access, Context.ConnectionAborted))
            throw new HubException("Not found");
    }

    protected Task LeaveGroupAsync(string group) =>
        base.Groups.RemoveFromGroupAsync(Context.ConnectionId, group, Context.ConnectionAborted);
}
