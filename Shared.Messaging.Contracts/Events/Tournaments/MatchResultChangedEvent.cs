namespace Shared.Messaging.Contracts.Events.Tournaments;

/// <summary>
/// The result of one match, whole: published when it is decided and again every time it changes —
/// the score corrected, a player-of-the-match pick made or taken back — and once more,
/// <see cref="MatchResultState.Withdrawn"/>, when the match is restarted or deleted. Each copy
/// replaces everything the last one said, so a consumer keeps the copy with the latest
/// <see cref="SnapshotAt"/> and drops any older one that arrives late. A republish of history sends
/// the same content with a newer <see cref="SnapshotAt"/>.
/// </summary>
/// <remarks>Carries user ids only; a person's name is resolved by whoever renders it.</remarks>
public record MatchResultChangedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid MatchEventId { get; init; }

    /// The division the match was drawn in, or the tournament itself when it has no divisions; absent for a
    /// match played outside any tournament.
    public Guid? TournamentId { get; init; }

    /// <inheritdoc cref="TournamentResultsChangedEvent.Name"/>
    public string? TournamentName { get; init; }

    public required MatchResultState State { get; init; }
    public required MatchResultEntrant Home { get; init; }
    public required MatchResultEntrant Away { get; init; }

    /// Set by set, in the order played. Empty when withdrawn, and for a walkover nobody played.
    public required IReadOnlyList<MatchSetScore> Sets { get; init; }

    /// Absent when withdrawn, and for a match that ended with nobody ahead.
    public MatchSide? Winner { get; init; }

    /// Awarded to the side that stayed rather than played out: the other side forfeited it.
    public required bool Walkover { get; init; }

    /// Each side's pick of the other side's best player, for the players who have an account.
    public required IReadOnlyList<MatchMvpPick> MvpPicks { get; init; }

    /// The player who kept the score or scouted the match, when they have an account.
    public Guid? ScorerUserId { get; init; }

    /// <summary>
    /// When the match was played: the end of its slot on the timetable. Read from the match rather
    /// than the clock, so a republish dates the result exactly as the first copy did. Absent when
    /// withdrawn.
    /// </summary>
    public DateTime? DecidedAt { get; init; }

    /// When this snapshot was taken, and so its version.
    public required DateTime SnapshotAt { get; init; }
}

public enum MatchResultState
{
    Decided,
    Withdrawn
}

public enum MatchSide
{
    Home,
    Away
}

public record MatchResultEntrant
{
    public required Guid EntrantId { get; init; }

    /// The team's name as entered, never a person's.
    public required string Name { get; init; }

    /// The club team the entrant stands for; absent for a squad the organizer named.
    public Guid? TeamId { get; init; }

    /// <summary>
    /// The players who have an account: the side's line-up for this match, or its tournament
    /// roster where it never named one.
    /// </summary>
    public required IReadOnlyList<Guid> PlayerUserIds { get; init; }
}

public record MatchSetScore
{
    public required int Home { get; init; }
    public required int Away { get; init; }
}

public record MatchMvpPick
{
    public required Guid PlayerUserId { get; init; }

    /// The side the player played for.
    public required Guid EntrantId { get; init; }
}
