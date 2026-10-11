namespace Shared.Security.Turnstile;

public sealed class TurnstileSettings
{
    public const string SectionName = "Turnstile";

    /// <summary>Unset, no challenge is checked: a development machine needs no Cloudflare account.</summary>
    public string? SecretKey { get; set; }
}
