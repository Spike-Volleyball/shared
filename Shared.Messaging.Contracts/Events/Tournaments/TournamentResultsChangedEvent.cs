namespace Shared.Messaging.Contracts.Events.Tournaments;

/// <summary>
/// The final result of one division, whole: published when the division completes and again every
/// time the result changes — a standing overridden, a roster corrected, a player of the tournament
/// named — and once more, <see cref="TournamentResultState.Withdrawn"/>, when the tournament is
/// deleted. Each copy replaces everything the last one said, so a consumer keeps the copy with the
/// latest <see cref="SnapshotAt"/> and drops any older one that arrives late. A republish of history
/// sends the same content with a newer <see cref="SnapshotAt"/>.
/// </summary>
/// <remarks>
/// A withdrawn copy names the division and nothing else: no field, no placings, no player of the
/// tournament. Carries user ids only; a person's name is resolved by whoever renders it.
/// </remarks>
public record TournamentResultsChangedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// The division, or the tournament itself when it has no divisions.
    public required Guid TournamentId { get; init; }

    public Guid? ParentTournamentId { get; init; }

    /// The club the tournament is run for.
    public Guid? ClubId { get; init; }

    /// "Parent · Division" for a division, the tournament's own name otherwise.
    public required string Name { get; init; }

    /// Absent only for a tournament created before tournaments had days of play.
    public DateTime? StartDate { get; init; }

    /// <inheritdoc cref="StartDate"/>
    public DateTime? EndDate { get; init; }

    public required bool IsVerified { get; init; }
    public required TournamentResultState State { get; init; }

    /// <summary>
    /// How many teams were drawn into the division's stages, whatever became of them afterwards: a
    /// team that withdrew halfway through still made up the field it withdrew from.
    /// </summary>
    public required int FieldSize { get; init; }

    /// <summary>
    /// Where the last stage played left everyone, best first. Entrants the draw does not separate
    /// share a position — two losing semi-finalists with no third-place match are both third. Empty
    /// when the last stage cannot name a podium: several groups, or a final never decided.
    /// </summary>
    public required IReadOnlyList<TournamentPlacing> Placings { get; init; }

    /// The player of the tournament the organizer named, when they have an account.
    public Guid? MvpUserId { get; init; }

    /// When this snapshot was taken, and so its version.
    public required DateTime SnapshotAt { get; init; }
}

public enum TournamentResultState
{
    Final,
    Withdrawn
}

public record TournamentPlacing
{
    public required Guid EntrantId { get; init; }

    /// The team's name as entered, never a person's.
    public required string EntrantName { get; init; }

    public required int Position { get; init; }

    /// The club team the entrant stands for; absent for a squad the organizer named.
    public Guid? TeamId { get; init; }

    public Guid? ClubId { get; init; }

    /// The rostered players who have an account. A guest on the roster has none, and is left out.
    public required IReadOnlyList<Guid> PlayerUserIds { get; init; }
}
