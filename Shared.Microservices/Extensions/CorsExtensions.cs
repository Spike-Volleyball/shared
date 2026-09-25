using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Microservices.Extensions;

public static class CorsExtensions
{
    private const string FrontendPolicy = "AllowFrontend";

    // Without Access-Control-Max-Age a browser keeps a preflight's answer for five seconds, so
    // the web app sent almost one preflight per API call. Two hours is Chromium's ceiling.
    private static readonly TimeSpan PreflightMaxAge = TimeSpan.FromHours(2);

    /// <summary>
    /// Lets the web app call this service with credentials from the origins in
    /// Cors:AllowedOrigins (the local web app when none are configured). Any header is allowed, so
    /// the traceparent, sentry-trace and baggage headers clients add pass too.
    /// </summary>
    public static IServiceCollection AddFrontendCors(this IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000"];

        return services.AddCors(options => options.AddPolicy(FrontendPolicy, policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetPreflightMaxAge(PreflightMaxAge)));
    }

    public static IApplicationBuilder UseFrontendCors(this IApplicationBuilder app) =>
        app.UseCors(FrontendPolicy);
}
