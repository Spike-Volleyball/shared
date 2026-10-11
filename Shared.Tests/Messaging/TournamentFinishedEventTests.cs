using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shared.Messaging.Contracts.Events.Tournaments;

namespace Shared.Tests.Messaging;

[TestFixture]
[Category("Unit")]
public class TournamentFinishedEventTests
{
    public sealed class NoticeLog
    {
        public TaskCompletionSource<TournamentFinishedEvent> Received { get; } = new();
    }

    public sealed class NoticeConsumer(NoticeLog log) : IConsumer<TournamentFinishedEvent>
    {
        public Task Consume(ConsumeContext<TournamentFinishedEvent> context)
        {
            log.Received.TrySetResult(context.Message);
            return Task.CompletedTask;
        }
    }

    [Test]
    public void TournamentFinishedEvent_IsAUserNotification()
    {
        typeof(TournamentFinishedEvent).Should().Implement<IUserNotification>();
    }

    [TestCase(1, false, 8)]
    [TestCase(3, true, 8)]
    [TestCase(null, false, 12)]
    public async Task Publish_ANoticeForASquad_ArrivesWithThePlaceItWasGiven(int? place, bool sharedPlace, int fieldSize)
    {
        // Arrange
        var log = new NoticeLog();
        await using var provider = new ServiceCollection()
            .AddSingleton(log)
            .AddMassTransitTestHarness(x => x.AddConsumer<NoticeConsumer>())
            .BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        var notice = new TournamentFinishedEvent
        {
            RecipientUserIds = [Guid.NewGuid(), Guid.NewGuid()],
            RecipientEmails = ["captain@example.com"],
            Title = "Autumn Showdown is finished",
            Body = "Kenton Kites finished 3rd of 8. Final standings are out.",
            DeepLink = "https://volleyspike.app/tournaments/4a1f6c1e-9a0e-4d1e-8b53-2f6c2d8c1b11",
            Category = "Events",
            TournamentId = Guid.NewGuid(),
            TournamentName = "Autumn Showdown",
            EntrantId = Guid.NewGuid(),
            Place = place,
            SharedPlace = sharedPlace,
            FieldSize = fieldSize,
        };

        // Act
        await harness.Bus.Publish(notice);
        var received = await log.Received.Task.WaitAsync(TimeSpan.FromSeconds(15));

        // Assert
        received.Should().BeEquivalentTo(notice);
        await harness.Stop();
    }
}
