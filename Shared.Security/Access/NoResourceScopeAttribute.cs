namespace Shared.Security.Access;

/// <summary>
/// For a route that names an id but deliberately checks no resource authority - the caller's own
/// <c>/me</c> data, or an id the action authorizes some other way. The reason is reviewed with
/// the route, so skipping the check is a decision rather than an omission.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class NoResourceScopeAttribute : Attribute
{
    public NoResourceScopeAttribute(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Say why this route checks no resource.", nameof(reason));

        Reason = reason;
    }

    public string Reason { get; }
}
