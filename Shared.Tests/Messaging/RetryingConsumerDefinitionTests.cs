using System.Collections.Concurrent;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shared.Messaging.Definitions;

namespace Shared.Tests.Messaging;

[TestFixture]
[Category("Unit")]
public class RetryingConsumerDefinitionTests
{
    public sealed record FlakyRequest(Guid Id);

    public sealed class AttemptLog
    {
        public ConcurrentDictionary<Guid, int> Attempts { get; } = new();
        public TaskCompletionSource Succeeded { get; } = new();
    }

    /// <summary>Fails the first attempt at every message and succeeds after that.</summary>
    public sealed class FlakyConsumer : IConsumer<FlakyRequest>
    {
        private readonly AttemptLog _log;

        public FlakyConsumer(AttemptLog log) => _log = log;

        public Task Consume(ConsumeContext<FlakyRequest> context)
        {
            var attempt = _log.Attempts.AddOrUpdate(context.Message.Id, 1, (_, previous) => previous + 1);
            if (attempt == 1)
                throw new InvalidOperationException("transient");

            _log.Succeeded.TrySetResult();
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task AddRetryingConsumer_TransientFailure_IsRetriedUntilItSucceeds()
    {
        // Arrange
        var log = new AttemptLog();
        await using var provider = new ServiceCollection()
            .AddSingleton(log)
            .AddMassTransitTestHarness(x => x.AddRetryingConsumer<FlakyConsumer>())
            .BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        var id = Guid.NewGuid();

        // Act
        await harness.Bus.Publish(new FlakyRequest(id));
        await log.Succeeded.Task.WaitAsync(TimeSpan.FromSeconds(15));

        // Assert
        log.Attempts[id].Should().Be(2);
        (await harness.Published.Any<Fault<FlakyRequest>>()).Should().BeFalse();
        await harness.Stop();
    }
}
