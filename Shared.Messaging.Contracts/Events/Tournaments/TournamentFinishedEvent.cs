namespace Shared.Messaging.Contracts.Events.Tournaments;

/// <summary>
/// Tells one squad its tournament is over and where it finished. Published ahead of the
/// <see cref="TournamentResultsChangedEvent"/> of the same completion, so a squad hears the
/// tournament is finished before the trophies that follow from its result.
/// </summary>
public record TournamentFinishedEvent : IUserNotification
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required IReadOnlyList<Guid> RecipientUserIds { get; init; }
    public IReadOnlyList<string> RecipientEmails { get; init; } = [];
    public required string Title { get; init; }
    public required string Body { get; init; }
    public required string DeepLink { get; init; }
    public required string Category { get; init; }

    /// <inheritdoc cref="TournamentResultsChangedEvent.TournamentId"/>
    public required Guid TournamentId { get; init; }

    /// <inheritdoc cref="TournamentResultsChangedEvent.Name"/>
    public required string TournamentName { get; init; }

    public required Guid EntrantId { get; init; }

    /// Absent when the draw places nobody: several groups, or a final never decided.
    public int? Place { get; init; }

    /// Other squads hold <see cref="Place"/> too: two losing semi-finalists with no third-place match.
    public required bool SharedPlace { get; init; }

    /// <inheritdoc cref="TournamentResultsChangedEvent.FieldSize"/>
    public required int FieldSize { get; init; }
}
