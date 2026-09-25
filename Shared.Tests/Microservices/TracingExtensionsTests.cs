using System.Diagnostics;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using Shared.Microservices.Extensions;

namespace Shared.Tests.Microservices;

[TestFixture]
[Category("Unit")]
public class TracingExtensionsTests
{
    private const string EfCoreSource = "OpenTelemetry.Instrumentation.EntityFrameworkCore";

    private readonly List<Activity> _exported = [];
    private SqliteConnection _connection = null!;
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private Activity? _requestActivity;

    [SetUp]
    public async Task SetUp()
    {
        _exported.Clear();
        _requestActivity = null;
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        // AddTracing always exports over OTLP too; a closed port keeps these spans out of any
        // collector running on this machine.
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://127.0.0.1:9";
        builder.Services.AddDbContext<ProbeDbContext>(options => options.UseSqlite(_connection));
        builder.Services.AddTracing(builder.Configuration, "tracing-tests", tracing => tracing
            .AddFilteredEfCoreInstrumentation()
            .AddInMemoryExporter(_exported));

        _app = builder.Build();
        _app.MapGet("/api/probe", async (ProbeDbContext db) =>
        {
            _requestActivity = Activity.Current;
            await db.Database.ExecuteSqlRawAsync("SELECT 1");
        });

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    [TearDown]
    public async Task TearDown()
    {
        _client.Dispose();
        await _app.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Test]
    public async Task AddFilteredEfCoreInstrumentation_CommandInARequest_IsExportedUnderTheRequestSpan()
    {
        // Act
        await _client.GetAsync("/api/probe");

        // Assert
        _requestActivity!.Recorded.Should().BeTrue();
        _exported.Should().ContainSingle(activity =>
            activity.Source.Name == EfCoreSource && activity.ParentSpanId == _requestActivity.SpanId);
    }

    [Test]
    public async Task AddFilteredEfCoreInstrumentation_CommandOutsideAnyRequest_IsNotExported()
    {
        // Arrange
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ProbeDbContext>();

        // Act
        await db.Database.ExecuteSqlRawAsync("SELECT 1");

        // Assert
        _exported.Should().NotContain(activity => activity.Source.Name == EfCoreSource);
    }

    public sealed class ProbeDbContext(DbContextOptions<ProbeDbContext> options) : DbContext(options);
}
