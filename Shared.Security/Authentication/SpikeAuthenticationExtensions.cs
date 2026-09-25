using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Microservices.Authorization;
using Shared.Options;
using Shared.Services;

namespace Shared.Security.Authentication;

public static class SpikeAuthenticationExtensions
{
    /// <summary>Where SignalR hubs are mapped, in every service.</summary>
    public const string HubPathPrefix = "/hubs";

    private const string JwtSection = "Jwt";

    /// <summary>
    /// User JWTs as the default scheme, and the admin console key as a second scheme that only the
    /// AdminConsole policy asks for. The one copy of the JWT setup every service's Startup carried,
    /// and so where the <see cref="RevocationCache" /> tokens are checked against comes from.
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
        services.TryAddSingleton(provider => new RevocationCache(UnprefixedCache(provider)));

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

    /// <summary>
    /// The service's own cache, unless that is a Redis cache putting a prefix before every key: then
    /// a second cache on the same Redis without one, where auth's keys are. Built on first use, so it
    /// follows whichever cache the service, or its test host, registered last.
    /// </summary>
    private static IDistributedCache UnprefixedCache(IServiceProvider provider)
    {
        var own = provider.GetRequiredService<IDistributedCache>();
        var redis = provider.GetRequiredService<IOptions<RedisCacheOptions>>().Value;
        if (own is not RedisCache || string.IsNullOrEmpty(redis.InstanceName))
            return own;

        return new RedisCache(new OptionsWrapper<RedisCacheOptions>(new RedisCacheOptions
        {
            Configuration = redis.Configuration,
            ConfigurationOptions = redis.ConfigurationOptions,
            ConnectionMultiplexerFactory = redis.ConnectionMultiplexerFactory
        }));
    }
}
