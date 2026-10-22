namespace Shared.Messaging.Contracts.Events.Events;

/// <summary>
/// A coach shared a side's lineup with its players: until they do, a player is not shown whether they
/// start. Addressed to the side's rostered players who have not declined, and sent once, when the
/// lineup goes from not shared to shared.
/// </summary>
public record MatchLineupSharedEvent : IEventNotice
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required IReadOnlyList<Guid> RecipientUserIds { get; init; }

    /// Always empty: everyone told is a rostered player with an account.
    public IReadOnlyList<string> RecipientEmails { get; init; } = [];
    public required string Title { get; init; }
    public required string Body { get; init; }
    public required string DeepLink { get; init; }
    public required string Category { get; init; }

    public required Guid TargetEventId { get; init; }
    public required string EventName { get; init; }

    /// The side whose lineup it is.
    public required Guid MatchTeamId { get; init; }

    /// The team behind the side; absent for a side the organiser typed in by hand.
    public Guid? TeamId { get; init; }
    public required string TeamName { get; init; }

    public Guid? SharedByUserId { get; init; }
}
