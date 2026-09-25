using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace Shared.Microservices.Extensions;

public static class MetricsExtensions
{
    /// <summary>
    /// Adds Prometheus metrics with HTTP request instrumentation and a dedicated metrics server.
    /// The metrics server port is the service port + 10000 (e.g., 5005 → 15005).
    /// </summary>
    public static IServiceCollection AddPrometheusMetrics(this IServiceCollection services,
        IConfiguration configuration)
    {
        var metricsPort = GetMetricsPort(configuration);
        services.AddSingleton(new MetricsServerOptions { Port = metricsPort });
        services.AddHostedService<MetricsServerHostedService>();

        return services;
    }

    /// <summary>
    /// Adds Prometheus HTTP request metrics middleware to the pipeline.
    /// Call this early in the middleware pipeline (after CORS, before routing).
    /// </summary>
    public static IApplicationBuilder UsePrometheusMetrics(this IApplicationBuilder app)
    {
        app.UseWhen(
            UserTraffic.Includes,
            branch => branch.UseHttpMetrics(options =>
            {
                options.ReduceStatusCodeCardinality();
            })
        );

        return app;
    }

    private static int GetMetricsPort(IConfiguration configuration)
    {
        var urls = configuration["ASPNETCORE_URLS"]
                   ?? configuration["urls"]
                   ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
                   ?? "http://+:5000";

        // Take the first URL (before any semicolons) and extract its port
        var firstUrl = urls.Split(';')[0].Trim();
        var port = 5000;
        var lastColon = firstUrl.LastIndexOf(':');
        if (lastColon >= 0 && int.TryParse(firstUrl[(lastColon + 1)..], out var parsed))
        {
            port = parsed;
        }

        return port + 10000;
    }
}

internal class MetricsServerOptions
{
    public int Port { get; init; }
}

internal class MetricsServerHostedService : IHostedService
{
    private readonly ILogger<MetricsServerHostedService> _logger;
    private readonly MetricsServerOptions _options;
    private readonly KestrelMetricServer _server;

    public MetricsServerHostedService(MetricsServerOptions options,
        ILogger<MetricsServerHostedService> logger)
    {
        _logger = logger;
        _options = options;
        _server = new KestrelMetricServer(port: options.Port);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _server.Start();
        _logger.LogInformation("Prometheus metrics server listening on port {Port}", _options.Port);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _server.StopAsync();
    }
}
