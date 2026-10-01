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
                // EF logs a command the database refuses as an error with no exception, and then
                // the save or query that ran it as another, with it. One failure is one event:
                // the command stays on the second as a breadcrumb, which the Serilog sink adds
                // whether or not the event went out.
                options.SetBeforeSend(@event =>
                    @event.Logger == LogSourceNames.DatabaseCommand ? null : @event);
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
