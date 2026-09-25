namespace Shared.Microservices.Authorization;

/// <summary>
/// The one comparison of a presented admin console key against the configured secret.
/// </summary>
/// <remarks>
/// Shared by the filter and the authentication scheme on purpose; the comparison itself is
/// <see cref="SharedSecret"/>'s.
/// </remarks>
public static class AdminConsoleKey
{
    public const string HeaderName = "X-Admin-Console-Key";

    public static bool Matches(AdminConsoleSettings settings, string presented) =>
        SharedSecret.Matches(settings.ApiKey, presented);
}
