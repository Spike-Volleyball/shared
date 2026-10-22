using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shared.Messaging.Contracts.Events.Events;
using Shared.Messaging.Contracts.Events.Tournaments;

namespace Shared.Tests.Messaging;

[TestFixture]
[Category("Unit")]
public class MatchTeamNoticesTests
{
    public sealed class NoticeLog
    {
        public TaskCompletionSource<MatchTeamInvitedEvent> Invited { get; } = new();
        public TaskCompletionSource<MatchTeamAnsweredEvent> Answered { get; } = new();
        public TaskCompletionSource<MatchLineupSharedEvent> Shared { get; } = new();
    }

    public sealed class InvitedConsumer(NoticeLog log) : IConsumer<MatchTeamInvitedEvent>
    {
        public Task Consume(ConsumeContext<MatchTeamInvitedEvent> context)
        {
            log.Invited.TrySetResult(context.Message);
            return Task.CompletedTask;
        }
    }

    public sealed class AnsweredConsumer(NoticeLog log) : IConsumer<MatchTeamAnsweredEvent>
    {
        public Task Consume(ConsumeContext<MatchTeamAnsweredEvent> context)
        {
            log.Answered.TrySetResult(context.Message);
            return Task.CompletedTask;
        }
    }

    public sealed class SharedConsumer(NoticeLog log) : IConsumer<MatchLineupSharedEvent>
    {
        public Task Consume(ConsumeContext<MatchLineupSharedEvent> context)
        {
            log.Shared.TrySetResult(context.Message);
            return Task.CompletedTask;
        }
    }

    [TestCase(typeof(MatchTeamInvitedEvent))]
    [TestCase(typeof(MatchTeamAnsweredEvent))]
    [TestCase(typeof(MatchLineupSharedEvent))]
    [TestCase(typeof(MatchResultPublishedEvent))]
    public void EveryNoticeAboutAMatch_IsAUserNotificationThatNamesItsEvent(Type notice)
    {
        notice.Should().Implement<IUserNotification>();
        notice.Should().Implement<IEventNotice>();
    }

    [Test]
    public async Task Publish_AnInvitation_ArrivesNamingTheEventAndTheTeamAsked()
    {
        // Arrange
        var log = new NoticeLog();
        await using var provider = Bus(log);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        var notice = new MatchTeamInvitedEvent
        {
            RecipientUserIds = [Guid.NewGuid(), Guid.NewGuid()],
            Title = "Pink Panthers invited Riverside Otters Women to a match",
            Body = "Sat 24 Oct, 14:00 · Walker Activity Dome",
            DeepLink = "https://volleyspike.app/events/4a1f6c1e-9a0e-4d1e-8b53-2f6c2d8c1b11",
            Category = "Invitations",
            TargetEventId = Guid.NewGuid(),
            EventName = "Pinks v Otters",
            MatchTeamId = Guid.NewGuid(),
            TeamId = Guid.NewGuid(),
            TeamName = "Riverside Otters Women",
            InvitedByUserId = Guid.NewGuid(),
        };

        // Act
        await harness.Bus.Publish(notice);
        var received = await log.Invited.Task.WaitAsync(TimeSpan.FromSeconds(15));

        // Assert
        received.Should().BeEquivalentTo(notice);
        await harness.Stop();
    }

    [TestCase(MatchTeamAnswer.Confirmed)]
    [TestCase(MatchTeamAnswer.Declined)]
    [TestCase(MatchTeamAnswer.Withdrawn)]
    public async Task Publish_AnAnswer_ArrivesSayingWhichItWasAndWhoGaveIt(MatchTeamAnswer answer)
    {
        // Arrange
        var log = new NoticeLog();
        await using var provider = Bus(log);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        var notice = new MatchTeamAnsweredEvent
        {
            RecipientUserIds = [Guid.NewGuid()],
            Title = "Riverside Otters Women is in",
            Body = "Pinks v Otters",
            DeepLink = "https://volleyspike.app/events/4a1f6c1e-9a0e-4d1e-8b53-2f6c2d8c1b11",
            Category = "Invitations",
            TargetEventId = Guid.NewGuid(),
            EventName = "Pinks v Otters",
            MatchTeamId = Guid.NewGuid(),
            TeamId = Guid.NewGuid(),
            TeamName = "Riverside Otters Women",
            Answer = answer,
            AnsweredByUserId = Guid.NewGuid(),
        };

        // Act
        await harness.Bus.Publish(notice);
        var received = await log.Answered.Task.WaitAsync(TimeSpan.FromSeconds(15));

        // Assert
        received.Answer.Should().Be(answer);
        received.Should().BeEquivalentTo(notice);
        await harness.Stop();
    }

    [Test]
    public async Task Publish_ASharedLineup_ArrivesNamingTheSideAndWhoSharedIt()
    {
        // Arrange
        var log = new NoticeLog();
        await using var provider = Bus(log);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        var notice = new MatchLineupSharedEvent
        {
            RecipientUserIds = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()],
            Title = "Tom Harris shared the Pink Panthers lineup for Pinks v Claws",
            Body = "Sat 24 Oct, 14:00 · Walker Activity Dome",
            DeepLink = "https://volleyspike.app/events/4a1f6c1e-9a0e-4d1e-8b53-2f6c2d8c1b11",
            Category = "Events",
            TargetEventId = Guid.NewGuid(),
            EventName = "Pinks v Claws",
            MatchTeamId = Guid.NewGuid(),
            TeamId = Guid.NewGuid(),
            TeamName = "Pink Panthers",
            SharedByUserId = Guid.NewGuid(),
        };

        // Act
        await harness.Bus.Publish(notice);
        var received = await log.Shared.Task.WaitAsync(TimeSpan.FromSeconds(15));

        // Assert
        received.Should().BeEquivalentTo(notice);
        await harness.Stop();
    }

    private static ServiceProvider Bus(NoticeLog log) => new ServiceCollection()
        .AddSingleton(log)
        .AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<InvitedConsumer>();
            x.AddConsumer<AnsweredConsumer>();
            x.AddConsumer<SharedConsumer>();
        })
        .BuildServiceProvider(true);
}
