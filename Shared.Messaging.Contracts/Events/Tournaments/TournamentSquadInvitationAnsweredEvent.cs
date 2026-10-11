namespace Shared.Messaging.Contracts.Events.Tournaments;

/// <summary>
/// A player answered a squad's invitation. Addressed to the people who run the squad, the one who
/// asked and its captain; the player who answered is never among them.
/// </summary>
public record TournamentSquadInvitationAnsweredEvent : IUserNotification
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
    public required Guid EntrantId { get; init; }
    public required string EntrantName { get; init; }

    /// False for a player who said no.
    public required bool Accepted { get; init; }

    /// The account that answered. None for a no said from the emailed link without signing in.
    public Guid? ActorUserId { get; init; }
}
