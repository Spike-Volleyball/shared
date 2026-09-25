using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sentry.AspNetCore;

namespace Shared.Logging.Extensions;

public static class WebHostBuilderExtensions
{
    /// <summary>
    /// Configures Sentry error tracking on the web host.
    /// Reads DSN from Configuration["Sentry:Dsn"] or env var Sentry__Dsn.
    /// If no DSN is configured, Sentry is disabled automatically.
    /// </summary>
    public static IWebHostBuilder UseSharedSentry(this IWebHostBuilder builder)
    {
        return builder
            .UseSentry(options =>
            {
                options.SendDefaultPii = false;
                options.AttachStacktrace = true;
                options.MinimumBreadcrumbLevel = LogLevel.Information;
                options.MinimumEventLevel = LogLevel.Error;
                // Errors only: backend traces live in Tempo. A zero rate alone is not enough,
                // because Sentry keeps the sampling decision of an incoming sentry-trace or
                // traceparent header, so its tracing middleware is not registered at all.
                options.TracesSampleRate = 0;
                options.AutoRegisterTracing = false;
            })
            .ConfigureServices(services =>
            {
                // PostConfigure runs AFTER config binding, so DSN from env/config is available.
                // If still null (no DSN configured), set to empty string to disable gracefully.
                services.PostConfigure<SentryAspNetCoreOptions>(options =>
                {
                    if (string.IsNullOrEmpty(options.Dsn))
                    {
                        options.Dsn = "";
                    }
                });
            });
    }
}
