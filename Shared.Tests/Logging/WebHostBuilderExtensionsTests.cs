using System.Collections.Concurrent;
using System.Diagnostics;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sentry;
using Sentry.AspNetCore;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;
using Shared.Logging.Extensions;

namespace Shared.Tests.Logging;

[TestFixture]
[Category("Unit")]
public class WebHostBuilderExtensionsTests
{
    private RecordingTransport _transport = null!;
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    [SetUp]
    public async Task SetUp()
    {
        _transport = new RecordingTransport();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer().UseSharedSentry();
        builder.Services.Configure<SentryAspNetCoreOptions>(options =>
        {
            options.Dsn = "https://key@o0.ingest.sentry.io/0";
            options.Transport = _transport;
        });

        _app = builder.Build();
        _app.UseRouting();
        _app.MapGet("/api/probe", () => "ok");
        _app.MapGet("/api/failing", (ILogger<WebHostBuilderExtensionsTests> logger) =>
            logger.LogError(new InvalidOperationException("probe"), "The probe failed"));

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    [TearDown]
    public async Task TearDown()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [TestCase("sentry-trace", "{0}-{1}-1")]
    [TestCase("traceparent", "00-{0}-{1}-01")]
    public async Task UseSharedSentry_RequestInATraceTheClientSampled_SendsNoTransaction(
        string header, string format)
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/probe");
        request.Headers.Add(header,
            string.Format(format, ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom()));

        // Act
        await _client.SendAsync(request);
        await FlushAsync();

        // Assert
        _transport.ItemTypes.Should().NotContain("transaction");
    }

    [Test]
    public async Task UseSharedSentry_ErrorLoggedInARequest_IsSent()
    {
        // Act
        await _client.GetAsync("/api/failing");
        await FlushAsync();

        // Assert
        _transport.ItemTypes.Should().Contain("event");
    }

    private Task FlushAsync() =>
        _app.Services.GetRequiredService<IHub>().FlushAsync(TimeSpan.FromSeconds(5));

    private sealed class RecordingTransport : ITransport
    {
        private readonly ConcurrentQueue<string?> _itemTypes = new();

        public IReadOnlyCollection<string?> ItemTypes => _itemTypes;

        public Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
        {
            foreach (var item in envelope.Items)
                _itemTypes.Enqueue(item.TryGetType());

            return Task.CompletedTask;
        }
    }
}
