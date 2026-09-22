using Microsoft.AspNetCore.Authorization;

namespace Shared.Security.Authorization;

/// <summary>
/// For the endpoints the admin console calls. Put it on its own controller: authorization data
/// combines, so a controller-level [Authorize] beside it would also demand a user token.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AdminConsoleOnlyAttribute : AuthorizeAttribute
{
    public AdminConsoleOnlyAttribute() : base(SpikePolicies.AdminConsole)
    {
    }
}
