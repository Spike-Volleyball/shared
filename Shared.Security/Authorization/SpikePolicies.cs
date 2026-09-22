namespace Shared.Security.Authorization;

public static class SpikePolicies
{
    /// <summary>The admin console's shared secret, and nothing else. A user token never passes it.</summary>
    public const string AdminConsole = "AdminConsole";
}
