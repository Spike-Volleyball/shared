using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Prometheus;
using Shared.Microservices.Extensions;

namespace Shared.Tests.Microservices;

[TestFixture]
[Category("Unit")]
public class MetricsExtensionsTests
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    [SetUp]
    public async Task SetUp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        _app = builder.Build();
        _app.UsePrometheusMetrics();
        _app.MapGet("/api/measured", () => "ok");
        _app.MapGet("/health", () => "ok");
        _app.MapGet("/metrics", () => "ok");
        _app.MapGet("/hubs/probe", () => "ok");

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    [TearDown]
    public async Task TearDown()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Test]
    public async Task UsePrometheusMetrics_UserRequest_IsCounted()
    {
        // Act
        await _client.GetAsync("/api/measured");

        // Assert
        (await CountedEndpointsAsync()).Should().Contain("/api/measured");
    }

    [TestCase("/health")]
    [TestCase("/metrics")]
    [TestCase("/hubs/probe")]
    public async Task UsePrometheusMetrics_RequestThatIsNotUserTraffic_IsNotCounted(string path)
    {
        // Act
        await _client.GetAsync(path);

        // Assert
        (await CountedEndpointsAsync()).Should().NotContain(path);
    }

    private static async Task<IReadOnlyList<string>> CountedEndpointsAsync()
    {
        using var exposition = new MemoryStream();
        await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(exposition);

        return Encoding.UTF8.GetString(exposition.ToArray())
            .Split('\n')
            .Where(line => line.StartsWith("http_requests_received_total{"))
            .Select(line => line.Split("endpoint=\"")[1].Split('"')[0])
            .ToList();
    }
}
