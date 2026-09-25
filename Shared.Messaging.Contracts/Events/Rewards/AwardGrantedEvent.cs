namespace Shared.Messaging.Contracts.Events.Rewards;

/// <summary>
/// A player has a new award, one of theirs has moved — a corrected result turned their silver
/// into gold — or a pin of theirs levelled up. Published by rewards-service for grants that are not quiet; notifications-service
/// turns it into the push that opens the unbox screen, in the words given here.
/// </summary>
public record AwardGrantedEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// The recipient.
    public required Guid UserId { get; init; }

    public required Guid AwardId { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public required AwardChange Change { get; init; }
}

public enum AwardChange
{
    Granted,
    Moved,

    /// A pin that levels up reached its next level: bronze to silver, silver to gold.
    LevelledUp
}
