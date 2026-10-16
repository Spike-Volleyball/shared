namespace Shared.Messaging.Contracts.Events.Tournaments;

/// <summary>
/// A squad was named to referee a match it does not play in, or taken off it: told to that squad
/// alone, so it knows where to be and when.
/// </summary>
public record TournamentMatchRefereeAssignedEvent : IUserNotification
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required IReadOnlyList<Guid> RecipientUserIds { get; init; }
    public IReadOnlyList<string> RecipientEmails { get; init; } = [];
    public required string Title { get; init; }
    public required string Body { get; init; }
    public required string DeepLink { get; init; }
    public required string Category { get; init; }

    public required Guid TournamentId { get; init; }
    public required string TournamentName { get; init; }
    public required Guid MatchEventId { get; init; }
    public required Guid RefereeEntrantId { get; init; }

    /// <summary>False when the squad was taken off the match it had been named to.</summary>
    public required bool Assigned { get; init; }
    public DateTime? StartTime { get; init; }
    public string? CourtName { get; init; }
}
