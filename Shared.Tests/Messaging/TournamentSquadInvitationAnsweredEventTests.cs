using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shared.Messaging.Contracts.Events.Tournaments;

namespace Shared.Tests.Messaging;

[TestFixture]
[Category("Unit")]
public class TournamentSquadInvitationAnsweredEventTests
{
    public sealed class NoticeLog
    {
        public TaskCompletionSource<TournamentSquadInvitationAnsweredEvent> Received { get; } = new();
    }

    public sealed class NoticeConsumer(NoticeLog log) : IConsumer<TournamentSquadInvitationAnsweredEvent>
    {
        public Task Consume(ConsumeContext<TournamentSquadInvitationAnsweredEvent> context)
        {
            log.Received.TrySetResult(context.Message);
            return Task.CompletedTask;
        }
    }

    [Test]
    public void TournamentSquadInvitationAnsweredEvent_IsAUserNotification()
    {
        typeof(TournamentSquadInvitationAnsweredEvent).Should().Implement<IUserNotification>();
    }

    [TestCase(true, true)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public async Task Publish_AnAnswer_ArrivesSayingWhichItWasAndWhoGaveIt(bool accepted, bool answeredSignedIn)
    {
        // Arrange
        var log = new NoticeLog();
        await using var provider = new ServiceCollection()
            .AddSingleton(log)
            .AddMassTransitTestHarness(x => x.AddConsumer<NoticeConsumer>())
            .BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        var notice = new TournamentSquadInvitationAnsweredEvent
        {
            RecipientUserIds = [Guid.NewGuid(), Guid.NewGuid()],
            Title = accepted ? "Mia Baker joined Sets Offenders" : "Mia Baker declined to join Sets Offenders",
            Body = "Autumn Showdown",
            DeepLink = "https://volleyspike.app/hub/tournaments/squad/4a1f6c1e-9a0e-4d1e-8b53-2f6c2d8c1b11",
            Category = "Invitations",
            TournamentId = Guid.NewGuid(),
            TournamentName = "Autumn Showdown",
            EntrantId = Guid.NewGuid(),
            EntrantName = "Sets Offenders",
            Accepted = accepted,
            ActorUserId = answeredSignedIn ? Guid.NewGuid() : null,
        };

        // Act
        await harness.Bus.Publish(notice);
        var received = await log.Received.Task.WaitAsync(TimeSpan.FromSeconds(15));

        // Assert
        received.Should().BeEquivalentTo(notice);
        await harness.Stop();
    }
}
