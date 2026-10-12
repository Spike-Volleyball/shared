using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shared.Messaging.Contracts.Events.Tournaments;

namespace Shared.Tests.Messaging;

[TestFixture]
[Category("Unit")]
public class TournamentSquadChatSeatsChangedEventTests
{
    public sealed class SeatLog
    {
        public TaskCompletionSource<TournamentSquadChatSeatsChangedEvent> Received { get; } = new();
    }

    public sealed class SeatConsumer(SeatLog log) : IConsumer<TournamentSquadChatSeatsChangedEvent>
    {
        public Task Consume(ConsumeContext<TournamentSquadChatSeatsChangedEvent> context)
        {
            log.Received.TrySetResult(context.Message);
            return Task.CompletedTask;
        }
    }

    [Test]
    public void ASheetThatOnlyGainedPlayers_NamesNobodyRemoved()
    {
        // Act
        var change = new TournamentSquadChatSeatsChangedEvent
        {
            ChatId = Guid.NewGuid(),
            TournamentId = Guid.NewGuid(),
            EntrantId = Guid.NewGuid(),
            JoinedUserIds = [Guid.NewGuid()],
        };

        // Assert
        change.RemovedUserIds.Should().BeEmpty();
    }

    [Test]
    public async Task Publish_AChangeOfSeats_ArrivesWithBothListsAndTheChatTheyAreFor()
    {
        // Arrange
        var log = new SeatLog();
        await using var provider = new ServiceCollection()
            .AddSingleton(log)
            .AddMassTransitTestHarness(x => x.AddConsumer<SeatConsumer>())
            .BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        var change = new TournamentSquadChatSeatsChangedEvent
        {
            ChatId = Guid.NewGuid(),
            TournamentId = Guid.NewGuid(),
            EntrantId = Guid.NewGuid(),
            JoinedUserIds = [Guid.NewGuid(), Guid.NewGuid()],
            RemovedUserIds = [Guid.NewGuid()],
        };

        // Act
        await harness.Bus.Publish(change);
        var received = await log.Received.Task.WaitAsync(TimeSpan.FromSeconds(15));

        // Assert
        received.Should().BeEquivalentTo(change);
        await harness.Stop();
    }
}
