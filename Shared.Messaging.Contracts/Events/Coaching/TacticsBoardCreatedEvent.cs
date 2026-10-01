namespace Shared.Messaging.Contracts.Events.Coaching;

/// <summary>
/// A tactics board somebody made themselves: published the first time a board is saved, and never for
/// the starter boards a new shelf is seeded with. A consumer keeps the copy with the latest
/// <see cref="SnapshotAt"/>.
/// </summary>
public record TacticsBoardCreatedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid BoardId { get; init; }
    public required Guid CreatorUserId { get; init; }

    public required DateTime CreatedAt { get; init; }
    public required DateTime SnapshotAt { get; init; }
}
