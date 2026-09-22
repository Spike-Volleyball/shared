using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shared.Microservices.Authorization;
using Shared.Options;

namespace Shared.Security.Authentication;

public static class SpikeAuthenticationExtensions
{
    /// <summary>Where SignalR hubs are mapped, in every service.</summary>
    public const string HubPathPrefix = "/hubs";

    private const string JwtSection = "Jwt";

    /// <summary>
    /// User JWTs as the default scheme, and the admin console key as a second scheme that only the
    /// AdminConsole policy asks for. The one copy of the JWT setup every service's Startup carried.
    /// </summary>
    /// <remarks>
    /// Fails at startup without JWT configuration. Skipping authentication when the section is
    /// missing, as some services did, turns a configuration mistake into an open service.
    /// </remarks>
    public static AuthenticationBuilder AddSpikeAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtSection).Get<JwtSettings>();
        if (jwt is null || string.IsNullOrEmpty(jwt.Secret))
            throw new InvalidOperationException("JWT configuration is missing.");

        services.Configure<JwtSettings>(configuration.GetSection(JwtSection));
        services.Configure<AdminConsoleSettings>(configuration.GetSection(AdminConsoleSettings.SectionName));

        return services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwt.Secret)),
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                // A browser or phone cannot set headers on a WebSocket, so SignalR carries the token
                // in the query string. Read there only for hubs: anywhere else a token in a URL ends
                // up in logs and browser history.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken)
                            && context.HttpContext.Request.Path.StartsWithSegments(HubPathPrefix))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            })
            .AddScheme<AuthenticationSchemeOptions, AdminConsoleKeyAuthenticationHandler>(
                AdminConsoleKeyAuthenticationHandler.SchemeName, _ => { });
    }
}
