namespace Shared.Messaging.Contracts.Events.Events;

/// <summary>
/// A team was named as the other side of a match and nobody has answered for it yet. Addressed to
/// the people who run that team, who have no roster to be told through: they are the only address an
/// invited team has. The publisher words the title, the body and the page it opens.
/// </summary>
public record MatchTeamInvitedEvent : IEventNotice
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required IReadOnlyList<Guid> RecipientUserIds { get; init; }

    /// Always empty: everyone told has an account, as staff of the team or of its club.
    public IReadOnlyList<string> RecipientEmails { get; init; } = [];
    public required string Title { get; init; }
    public required string Body { get; init; }
    public required string DeepLink { get; init; }
    public required string Category { get; init; }

    public required Guid TargetEventId { get; init; }
    public required string EventName { get; init; }

    /// The side that was invited.
    public required Guid MatchTeamId { get; init; }

    /// The team behind the side; absent for a side the organiser typed in by hand, which nobody can be told for.
    public Guid? TeamId { get; init; }
    public required string TeamName { get; init; }

    public Guid? InvitedByUserId { get; init; }
}
