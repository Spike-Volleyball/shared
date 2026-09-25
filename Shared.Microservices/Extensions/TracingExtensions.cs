using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Shared.Microservices.Extensions;

public static class TracingExtensions
{
    /// <summary>
    /// Adds OpenTelemetry distributed tracing with OTLP export.
    /// Automatically instruments ASP.NET Core requests and outbound HTTP calls.
    /// The OTLP endpoint is configured via the OTEL_EXPORTER_OTLP_ENDPOINT env var
    /// (read automatically by the OTel SDK). Set it in docker-compose or .env files.
    /// Default (when unset): http://localhost:4317.
    ///
    /// Sampling: every request is traced, including one whose W3C traceparent says the caller
    /// did not sample it — web and mobile sample their own traces for their own budgets, and
    /// that must not cost the backend its trace. Work under a local span that was filtered out
    /// (a health probe) stays untraced. The sampler is set in code, so OTEL_TRACES_SAMPLER is
    /// ignored: sampling less means changing the root sampler here.
    ///
    /// Note on HttpClientInstrumentation: outbound HTTP calls (including Stripe API,
    /// S3 signed URLs) are captured as spans. Query string parameters may contain
    /// tokens or signed URL credentials. The OTel SDK does NOT redact these by default.
    /// This is an accepted risk for initial rollout — URL sanitization is tracked as
    /// a future enhancement.
    /// </summary>
    public static IServiceCollection AddTracing(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        Action<TracerProviderBuilder>? configure = null)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: serviceName,
                    serviceVersion: typeof(TracingExtensions).Assembly
                        .GetName().Version?.ToString() ?? "1.0.0"))
            .WithTracing(tracing =>
            {
                tracing
                    .SetSampler(new ParentBasedSampler(
                        new AlwaysOnSampler(),
                        remoteParentNotSampled: new AlwaysOnSampler()))
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        // The filter sees every host in the process, so this also drops the
                        // scrapes Prometheus makes to the separate metrics server.
                        options.Filter = UserTraffic.Includes;
                    })
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter();

                configure?.Invoke(tracing);
            });

        return services;
    }

    /// <summary>
    /// Adds EF Core instrumentation that only records spans when the current trace
    /// originated from an exported source (ASP.NET Core or HttpClient). This prevents:
    /// - MassTransit outbox polling (no parent activity at all)
    /// - MassTransit consumer DB queries (parent is MassTransit activity which isn't
    ///   exported, producing orphaned spans in Tempo)
    /// </summary>
    public static TracerProviderBuilder AddFilteredEfCoreInstrumentation(
        this TracerProviderBuilder tracing)
    {
        return tracing.AddEntityFrameworkCoreInstrumentation(options =>
        {
            // The instrumentation calls this with its own command span as Activity.Current, and only
            // when that span is recorded, so the span itself would always pass: start at its parent.
            options.Filter = (_, _) => HasExportedAncestor(Activity.Current?.Parent);
        });
    }

    private static bool HasExportedAncestor(Activity? activity)
    {
        while (activity != null)
        {
            if (activity.IsAllDataRequested)
                return true;
            activity = activity.Parent;
        }

        return false;
    }
}
