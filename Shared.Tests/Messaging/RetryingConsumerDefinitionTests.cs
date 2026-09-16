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
        public ConcurrentQueue<Guid> ScopeIds { get; } = new();
        public TaskCompletionSource Succeeded { get; } = new();
    }

    /// <summary>Stands in for a scoped DbContext: one instance per container scope.</summary>
    public sealed class ScopedWork
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    /// <summary>Fails the first attempt at every message and succeeds after that.</summary>
    public sealed class FlakyConsumer : IConsumer<FlakyRequest>
    {
        private readonly AttemptLog _log;
        private readonly ScopedWork _work;

        public FlakyConsumer(AttemptLog log, ScopedWork work)
        {
            _log = log;
            _work = work;
        }

        public Task Consume(ConsumeContext<FlakyRequest> context)
        {
            _log.ScopeIds.Enqueue(_work.Id);
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
        await using var provider = BuildHarness(log);
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

    /// <summary>
    /// What makes a retry safe for a consumer that saves through EF: a save that failed leaves its
    /// entities tracked, and an attempt on the same context would issue them again.
    /// </summary>
    [Test]
    public async Task AddRetryingConsumer_RetriedAttempt_GetsScopedDependenciesOfItsOwn()
    {
        // Arrange
        var log = new AttemptLog();
        await using var provider = BuildHarness(log);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        // Act
        await harness.Bus.Publish(new FlakyRequest(Guid.NewGuid()));
        await log.Succeeded.Task.WaitAsync(TimeSpan.FromSeconds(15));

        // Assert
        log.ScopeIds.Should().HaveCount(2).And.OnlyHaveUniqueItems();
        await harness.Stop();
    }

    private static ServiceProvider BuildHarness(AttemptLog log) =>
        new ServiceCollection()
            .AddSingleton(log)
            .AddScoped<ScopedWork>()
            .AddMassTransitTestHarness(x => x.AddRetryingConsumer<FlakyConsumer>())
            .BuildServiceProvider(true);
}
