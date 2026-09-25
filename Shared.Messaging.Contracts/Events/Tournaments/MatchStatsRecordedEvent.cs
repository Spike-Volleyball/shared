namespace Shared.Messaging.Contracts.Events.Tournaments;

/// <summary>
/// One match's touch stats per player, whole: published when a scouted match is decided and again
/// whenever its scout log changes afterwards, and once more, <see cref="MatchStatsState.Withdrawn"/>,
/// when the match is restarted or deleted. Each copy replaces the last; a consumer keeps the copy with
/// the latest <see cref="SnapshotAt"/>.
/// </summary>
public record MatchStatsRecordedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid MatchEventId { get; init; }
    public Guid? TournamentId { get; init; }
    public Guid? ClubId { get; init; }
    public required MatchStatsState State { get; init; }

    /// The day it was played, on the clock where it was played: "in one day" counts by this.
    public required DateOnly PlayedOn { get; init; }

    /// Players with an account and at least one scouted touch. Empty when withdrawn.
    public required IReadOnlyList<PlayerTouchStats> Players { get; init; }

    public required DateTime SnapshotAt { get; init; }
}

public enum MatchStatsState
{
    Recorded,
    Withdrawn
}

/// <summary>Counts from the scout log, by the grades the scout codes define.</summary>
public record PlayerTouchStats
{
    public required Guid UserId { get; init; }

    /// Serves scored outright (S#).
    public required int Aces { get; init; }

    /// Blocks that won the point (B#).
    public required int Blocks { get; init; }

    /// Attacks that won the point (A#).
    public required int Kills { get; init; }

    /// Perfect receptions (R#).
    public required int PerfectPasses { get; init; }

    /// Digs kept in play (D+ or D#).
    public required int Digs { get; init; }

    /// Sets a teammate put away for a kill in the same rally.
    public required int Assists { get; init; }
}
