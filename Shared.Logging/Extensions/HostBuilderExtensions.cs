using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Filters;

namespace Shared.Logging.Extensions;

public static class HostBuilderExtensions
{
    /// <summary>
    /// Configures Serilog with JSON console output.
    /// Call this on the IHostBuilder in Program.cs before ConfigureWebHostDefaults.
    /// </summary>
    public static IHostBuilder UseSharedSerilog(this IHostBuilder builder)
    {
        return builder.UseSerilog((context, services, loggerConfig) =>
        {
            loggerConfig
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
                .WriteTo.Console(new Serilog.Formatting.Compact.RenderedCompactJsonFormatter())
                // Serilog is the only logger once this runs, so the logger provider UseSharedSentry
                // registers never sees an ILogger call: without a sink of its own Sentry hears only
                // what escapes the whole pipeline, and ErrorHandlerMiddleware lets nothing escape.
                // A sub-logger, because Serilog hands it a copy of each event: the caller's address
                // stays in the log and is taken out of what Sentry is sent, as SendDefaultPii = false
                // in UseSharedSentry promises.
                .WriteTo.Logger(forSentry => forSentry
                    .Enrich.With<WithheldFromSentryEnricher>()
                    // EF logs a save the database refuses as an error before its caller has seen
                    // the exception: a caller that expects it (an insert race, a redelivered
                    // webhook) has failed nothing, and one that does not lets it through to be
                    // reported there. Not dropped later in BeforeSend: Sentry would remember the
                    // exception as sent and drop that report as its duplicate.
                    .Filter.ByExcluding(Matching.FromSource(LogSourceNames.DatabaseSave))
                    .WriteTo.Sentry(sentry =>
                    {
                        // UseSharedSentry configures the SDK, its DSN included. This only feeds it,
                        // and feeds nothing while no DSN is set.
                        sentry.InitializeSdk = false;
                        sentry.MinimumEventLevel = LogEventLevel.Error;
                        sentry.MinimumBreadcrumbLevel = LogEventLevel.Information;
                    }));

            // Override noisy loggers
            loggerConfig
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override(LogSourceNames.DatabaseCommand, LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Infrastructure", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning);
        });
    }
}
