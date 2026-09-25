namespace Shared.Models.Jwt;

/// <summary>
/// An account's session version. Auth mints it into every access token and moves it on whenever
/// all of the account's sessions must end: a new password, consent withdrawn, a suspension, a
/// deletion scheduled. Revoking the refresh tokens stops new access tokens being minted, but only
/// comparing versions stops the ones already issued before they expire (SPI-6495).
/// </summary>
public static class SessionVersions
{
    /// <summary>The claim an access token carries its version in.</summary>
    public const string ClaimType = "sessionVersion";

    /// <summary>Where every account starts, and what a token without the claim counts as.</summary>
    public const int Initial = 1;

    /// <summary>
    /// Where auth records an account's version once it has moved on, for every service to compare
    /// tokens with. The record outlives every token minted before the move, so no record means
    /// there is nothing to refuse.
    /// </summary>
    public static string CacheKey(string userId) => $"session_version:{userId}";
}
