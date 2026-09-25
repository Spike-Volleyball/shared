namespace Shared.Models;

/// <summary>
/// The addresses Spike makes up for accounts that have none of their own: a child a guardian adds,
/// and a person a club imports with no email. They keep auth's unique email index whole. None is a
/// mailbox, so nothing is ever sent to one, and none is taken as the address someone typed.
/// </summary>
public static class SyntheticEmail
{
    private const string ReservedDomain = "spike.local";
    private const string ManagedDomain = "managed." + ReservedDomain;
    private const string PlaceholderDomain = "placeholder." + ReservedDomain;

    public static string ForManaged(Guid userId) => $"managed-{userId:N}@{ManagedDomain}";

    public static string ForPlaceholder(Guid userId) => $"placeholder-{userId:N}@{PlaceholderDomain}";

    /// <summary>Whether the address is under spike.local, whatever it was made up for.</summary>
    public static bool IsSynthetic(string? email)
    {
        var at = email?.LastIndexOf('@') ?? -1;
        if (at < 0)
            return false;

        var domain = email![(at + 1)..].Trim();
        return domain.Equals(ReservedDomain, StringComparison.OrdinalIgnoreCase)
            || domain.EndsWith("." + ReservedDomain, StringComparison.OrdinalIgnoreCase);
    }
}
