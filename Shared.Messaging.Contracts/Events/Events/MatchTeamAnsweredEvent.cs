namespace Shared.Messaging.Contracts.Events.Events;

/// <summary>
/// A side of a match answered: it is in, it said no, or it dropped out. Addressed to the people who
/// run the match, never to the one who answered. Published whatever the match's Silent flag: it is
/// someone's own answer, passed on to the organiser.
/// </summary>
public record MatchTeamAnsweredEvent : IEventNotice
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required IReadOnlyList<Guid> RecipientUserIds { get; init; }

    /// Always empty: everyone told has an account, as the creator or a host of the match.
    public IReadOnlyList<string> RecipientEmails { get; init; } = [];
    public required string Title { get; init; }
    public required string Body { get; init; }
    public required string DeepLink { get; init; }
    public required string Category { get; init; }

    public required Guid TargetEventId { get; init; }
    public required string EventName { get; init; }

    /// The side that answered.
    public required Guid MatchTeamId { get; init; }

    /// The team behind the side; absent for a side the organiser typed in by hand.
    public Guid? TeamId { get; init; }
    public required string TeamName { get; init; }

    public required MatchTeamAnswer Answer { get; init; }

    /// The account that answered for the side.
    public Guid? AnsweredByUserId { get; init; }
}

public enum MatchTeamAnswer
{
    Confirmed,
    Declined,
    Withdrawn
}
