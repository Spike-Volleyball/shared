using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentry;
using Sentry.AspNetCore;
using Serilog.Context;
using Shared.Logging;
using Shared.Logging.Extensions;

namespace Shared.Tests.Logging;

/// <summary>
/// The host every service's Program.cs builds: UseSharedSerilog on the host, UseSharedSentry on
/// the web host, over Kestrel. Each half passes its own tests alone; what a service runs is the
/// two together, where Serilog is the only logger.
/// </summary>
[TestFixture]
[Category("Unit")]
public class HostBuilderExtensionsTests
{
    private const string Dsn = "https://key@o0.ingest.sentry.io/0";
    private const string CallerAddress = "203.0.113.7";

    private RecordingSentryTransport _transport = null!;
    private SqliteConnection _database = null!;
    private TextWriter _console = null!;
    private StringWriter _logged = null!;
    private IHost? _host;
    private HttpClient? _client;

    [SetUp]
    public void SetUp()
    {
        _transport = new RecordingSentryTransport();
        // In memory, and gone with its connection: one connection held open is the database.
        _database = new SqliteConnection("DataSource=:memory:");
        _database.Open();
        _console = Console.Out;
        _logged = new StringWriter();
        Console.SetOut(_logged);
    }

    [TearDown]
    public async Task TearDown()
    {
        _client?.Dispose();
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        _database.Dispose();
        Console.SetOut(_console);
        _logged.Dispose();
    }

    [Test]
    public async Task UseSharedSerilog_ErrorLoggedThroughILogger_ReachesSentryWithItsException()
    {
        // Arrange
        await StartAsync(Dsn);

        // Act
        await _client!.GetAsync("/failing");
        await FlushAsync();

        // Assert
        var exception = _transport.Events.Should().ContainSingle().Which
            .GetProperty("exception").GetProperty("values")[0];
        exception.GetProperty("type").GetString().Should().Be(typeof(InvalidOperationException).FullName);
        exception.GetProperty("value").GetString().Should().Be("probe");
    }

    [Test]
    public async Task UseSharedSerilog_WarningLogged_SendsNoEvent()
    {
        // Arrange
        await StartAsync(Dsn);

        // Act
        await _client!.GetAsync("/warning");
        await FlushAsync();

        // Assert
        _transport.Events.Should().BeEmpty();
    }

    [Test]
    public async Task UseSharedSerilog_NoDsnConfigured_SendsNothingAndLeavesSentryOff()
    {
        // Arrange
        await StartAsync(dsn: null);

        // Act
        await _client!.GetAsync("/failing");
        await FlushAsync();

        // Assert
        _transport.ItemTypes.Should().BeEmpty();
        _host!.Services.GetRequiredService<IHub>().IsEnabled.Should().BeFalse();
    }

    /// <summary>
    /// Sentry's middleware reports what escapes the pipeline, and Kestrel then logs the same
    /// exception as an error, which is now an event of its own unless the SDK recognises it.
    /// </summary>
    [Test]
    public async Task UseSharedSerilog_ExceptionThatEscapesThePipeline_IsReportedOnce()
    {
        // Arrange
        await StartAsync(Dsn);

        // Act
        var response = await _client!.GetAsync("/escaping");
        await FlushAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        _logged.ToString().Should().Contain("An unhandled exception was thrown by the application");
        _transport.Events.Should().ContainSingle();
    }

    [Test]
    public async Task UseSharedSerilog_ErrorLoggedWithTheCallersAddress_KeepsTheAddressInTheLogAndOutOfSentry()
    {
        // Arrange
        await StartAsync(Dsn);

        // Act
        await _client!.GetAsync("/failing-for-a-caller");
        await FlushAsync();

        // Assert
        _logged.ToString().Should().Contain($"\"{LogPropertyNames.ClientIp}\":\"{CallerAddress}\"");

        var sent = _transport.Events.Should().ContainSingle().Which;
        sent.GetRawText().Should().NotContain(CallerAddress);
        sent.GetProperty("extra").TryGetProperty("Subject", out _).Should().BeTrue();
    }

    /// <summary>
    /// EF logs a save the database refuses as an error before its caller has seen it, so the log
    /// cannot say whether it was handled. A caller that expects the refusal (an insert race it
    /// recovers from, a redelivered webhook) has failed nothing.
    /// </summary>
    [Test]
    public async Task UseSharedSerilog_SaveTheDatabaseRefusesAndTheCallerHandles_SendsNoEvent()
    {
        // Arrange
        await StartAsync(Dsn);

        // Act
        await _client!.GetAsync("/handled-failing-save");
        await FlushAsync();

        // Assert
        _logged.ToString().Should().Contain("Failed executing DbCommand")
            .And.Contain("An exception occurred in the database while saving changes");
        _transport.Events.Should().BeEmpty();
    }

    /// <summary>
    /// A refusal nobody handles is reported by whoever it escapes to, with the refused command as
    /// a breadcrumb: one failure, one event.
    /// </summary>
    [Test]
    public async Task UseSharedSerilog_SaveTheDatabaseRefusesAndNobodyHandles_IsOneEventCarryingTheCommandAsABreadcrumb()
    {
        // Arrange
        await StartAsync(Dsn);

        // Act
        await _client!.GetAsync("/unhandled-failing-save");
        await FlushAsync();

        // Assert
        var sent = _transport.Events.Should().ContainSingle().Which;
        sent.GetProperty("exception").GetProperty("values").EnumerateArray()
            .Select(exception => exception.GetProperty("type").GetString())
            .Should().Contain(typeof(DbUpdateException).FullName);

        sent.GetProperty("breadcrumbs").EnumerateArray()
            .Should().Contain(crumb =>
                crumb.GetProperty("category").GetString() == DbLoggerCategory.Database.Command.Name
                && crumb.GetProperty("message").GetString()!.StartsWith("Failed executing DbCommand"));
    }

    private async Task StartAsync(string? dsn)
    {
        _host = Host.CreateDefaultBuilder()
            .UseSharedSerilog()
            .ConfigureWebHostDefaults(webBuilder => webBuilder
                .UseKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0))
                .UseSharedSentry()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddDbContext<ProbeContext>(options => options.UseSqlite(_database));
                    services.Configure<SentryAspNetCoreOptions>(options =>
                    {
                        options.Dsn = dsn;
                        options.Transport = _transport;
                    });
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(MapProbes);
                }))
            .Build();

        using (var scope = _host.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<ProbeContext>().Database.EnsureCreatedAsync();

        await _host.StartAsync();

        var address = _host.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        _client = new HttpClient { BaseAddress = new Uri(address) };
    }

    private static void MapProbes(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/failing", (ILogger<HostBuilderExtensionsTests> logger) =>
            logger.LogError(new InvalidOperationException("probe"), "The probe failed for {Subject}", "a probe"));

        endpoints.MapGet("/warning", (ILogger<HostBuilderExtensionsTests> logger) =>
            logger.LogWarning("The probe is slow"));

        endpoints.MapGet("/escaping", IResult () => throw new InvalidOperationException("escaped"));

        endpoints.MapGet("/failing-for-a-caller", (ILogger<HostBuilderExtensionsTests> logger) =>
        {
            using (LogContext.PushProperty(LogPropertyNames.ClientIp, CallerAddress))
                logger.LogError(new InvalidOperationException("probe"), "The probe failed for {Subject}", "a probe");
        });

        endpoints.MapGet("/handled-failing-save", async (ProbeContext context) =>
        {
            try
            {
                await SaveDuplicateAsync(context);
            }
            catch (DbUpdateException)
            {
                // The caller of a real save decides what a refusal means; EF has logged it by now.
            }
        });

        endpoints.MapGet("/unhandled-failing-save", (ProbeContext context) => SaveDuplicateAsync(context));
    }

    private static async Task SaveDuplicateAsync(ProbeContext context)
    {
        context.Rows.Add(new ProbeRow { Id = 1 });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        context.Rows.Add(new ProbeRow { Id = 1 });
        await context.SaveChangesAsync();
    }

    private Task FlushAsync() =>
        _host!.Services.GetRequiredService<IHub>().FlushAsync(TimeSpan.FromSeconds(5));

    private sealed class ProbeContext(DbContextOptions<ProbeContext> options) : DbContext(options)
    {
        public DbSet<ProbeRow> Rows => Set<ProbeRow>();
    }

    private sealed class ProbeRow
    {
        public int Id { get; set; }
    }
}
