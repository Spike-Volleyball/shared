namespace Shared.Messaging.Contracts.Events.Events;

/// <summary>
/// The result of a match of its own, told to the people in it. The publisher words the title, the
/// body and the page it opens, as it does for a tournament match's result.
/// </summary>
public record MatchResultPublishedEvent : IEventNotice
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required IReadOnlyList<Guid> RecipientUserIds { get; init; }

    /// Always empty: everyone told has an account, as a participant or a follower of the match.
    public IReadOnlyList<string> RecipientEmails { get; init; } = [];
    public required string Title { get; init; }
    public required string Body { get; init; }
    public required string DeepLink { get; init; }
    public required string Category { get; init; }

    /// The match the result is of, which <see cref="EventId"/> cannot be: that one identifies this message.
    public required Guid TargetEventId { get; init; }
    public required string EventName { get; init; }

    /// The side that won; absent when nobody did.
    public Guid? WinnerEntrantId { get; init; }

    /// Sets won by each side.
    public int HomeScore { get; init; }
    public int AwayScore { get; init; }

    /// A result already told, changed: the winner is no longer the one the recipients were told.
    public bool IsCorrection { get; init; }
}
