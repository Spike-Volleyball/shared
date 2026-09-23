using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Shared.Extensions;
using Shared.Security.Authentication;

namespace Shared.Security.Authorization;

public static class SpikeAuthorizationExtensions
{
    /// <summary>
    /// Deny by default: an endpoint, hub or gRPC method that declares nothing requires a signed-in
    /// user. The only ways out are <see cref="PublicEndpointAttribute"/>, which says why, the
    /// <see cref="SpikePolicies.AdminConsole"/> policy, and the internal listener.
    /// </summary>
    /// <remarks>
    /// Without a fallback an action is anonymous unless it remembers to check, and the leak audit
    /// of 2026-09-22 found every service relying on that memory (SPI-6446).
    /// </remarks>
    public static IServiceCollection AddSpikeAuthorization(
        this IServiceCollection services, Action<AuthorizationOptions>? configure = null)
    {
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build();

            options.AddGuardianPolicies();

            options.AddPolicy(SpikePolicies.AdminConsole, policy => policy
                .AddAuthenticationSchemes(AdminConsoleKeyAuthenticationHandler.SchemeName)
                .RequireAuthenticatedUser());

            configure?.Invoke(options);
        });

        return services;
    }
}
