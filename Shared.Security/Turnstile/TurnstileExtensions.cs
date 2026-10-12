using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Security.Turnstile;

public static class TurnstileExtensions
{
    /// <summary>
    /// Binds the <see cref="TurnstileSettings.SectionName"/> section and registers the verifier with its own
    /// HttpClient. The timeout is Cloudflare's guidance for siteverify: the verifier fails open, so a slow
    /// Cloudflare must not hold a form for the default 100 seconds.
    /// </summary>
    public static IServiceCollection AddTurnstile(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TurnstileSettings>(configuration.GetSection(TurnstileSettings.SectionName));
        services.AddHttpClient<ITurnstileVerifier, TurnstileVerifier>(client => client.Timeout = TimeSpan.FromSeconds(10));
        return services;
    }
}
