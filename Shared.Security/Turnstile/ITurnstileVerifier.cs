namespace Shared.Security.Turnstile;

public interface ITurnstileVerifier
{
    /// <summary>
    /// Throws a 400 unless <paramref name="token"/> is a Cloudflare Turnstile challenge the visitor passed. Passes
    /// when no secret is configured, and when Cloudflare cannot be reached: a form that stops taking people during
    /// an upstream outage costs more than the bots it was keeping out.
    /// </summary>
    Task RequireChallengePassedAsync(string? token, string? clientIp);
}
