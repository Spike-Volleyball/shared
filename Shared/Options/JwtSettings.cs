namespace Shared.Options;

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;

    /// <summary>
    /// How long a service goes on using what it last read of an account's session version. Every
    /// signed-in request needs it, and asking Redis each time would add a round trip to every one;
    /// the price is that a session ended in auth is refused up to this long later.
    /// </summary>
    public int SessionVersionCacheSeconds { get; set; } = 30;
}