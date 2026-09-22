namespace Shared.Security.Access;

/// <summary>
/// Who may do what to one kind of resource, answered in one place. Routes ask it through
/// <see cref="AccessAttribute{TAccess}"/>, hubs through SecureHub, and business code directly, so
/// a sibling endpoint cannot quietly skip the rule its neighbour applies.
/// </summary>
/// <typeparam name="TAccess">The resource's own vocabulary of access - one enum per resource.</typeparam>
public interface IResourceAuthority<in TAccess> where TAccess : struct, Enum
{
    /// <param name="userId">Whose access is asked - the acted-as minor when a guardian acts for
    /// one - or null for a signed-out caller.</param>
    /// <returns>False for a resource that does not exist, too; callers cannot tell the two apart.</returns>
    Task<bool> CanAsync(Guid? userId, Guid resourceId, TAccess access, CancellationToken ct);
}
