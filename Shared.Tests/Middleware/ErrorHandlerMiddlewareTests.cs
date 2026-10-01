using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Middleware;

namespace Shared.Tests.Middleware;

[TestFixture]
[Category("Unit")]
public class ErrorHandlerMiddlewareTests
{
    private RecordingLogger _logger = null!;
    private DefaultHttpContext _context = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = new RecordingLogger();
        _context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        _context.Request.Method = HttpMethods.Get;
        _context.Request.Path = "/v1/households/family-invitations/incoming";
    }

    private ErrorHandlerMiddleware MiddlewareOver(RequestDelegate next) =>
        new(next, _logger, Substitute.For<IHostEnvironment>());

    [Test]
    public async Task Invoke_WorkCancelledBecauseTheCallerHungUp_IsNotAnErrorAndWritesNoBody()
    {
        // Arrange
        _context.RequestAborted = new CancellationToken(canceled: true);
        var sut = MiddlewareOver(context => Task.FromException(new OperationCanceledException(context.RequestAborted)));

        // Act
        await sut.Invoke(_context);

        // Assert
        _context.Response.StatusCode.Should().Be(StatusCodes.Status499ClientClosedRequest);
        _context.Response.Body.Length.Should().Be(0);
        _logger.Levels.Should().Equal(LogLevel.Information);
    }

    /// <summary>A timeout of the service's own making cancels work too; the caller is still waiting.</summary>
    [Test]
    public async Task Invoke_WorkCancelledWhileTheCallerStillWaits_IsAnInternalError()
    {
        // Arrange
        var sut = MiddlewareOver(_ => Task.FromException(new TaskCanceledException("The upstream call timed out")));

        // Act
        await sut.Invoke(_context);

        // Assert
        _context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        _context.Response.Body.Position = 0;
        (await JsonDocument.ParseAsync(_context.Response.Body)).RootElement.GetProperty("code").GetString()
            .Should().Be("INTERNAL_ERROR");
        _logger.Levels.Should().Equal(LogLevel.Error);
    }

    /// <summary>The caller hanging up does not excuse a failure that is not the cancellation.</summary>
    [Test]
    public async Task Invoke_FailureAfterTheCallerHungUp_IsStillLoggedAsAnError()
    {
        // Arrange
        _context.RequestAborted = new CancellationToken(canceled: true);
        var sut = MiddlewareOver(_ => Task.FromException(new InvalidOperationException("broken")));

        // Act
        await sut.Invoke(_context);

        // Assert
        _logger.Levels.Should().Equal(LogLevel.Error);
    }

    private sealed class RecordingLogger : ILogger<ErrorHandlerMiddleware>
    {
        private readonly List<LogLevel> _levels = [];

        public IReadOnlyList<LogLevel> Levels => _levels;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => _levels.Add(logLevel);
    }
}
