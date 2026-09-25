namespace Shared.Messaging.Contracts.Events.Events;

/// <summary>
/// A place on an event opened up and went to the person at the front of its waiting list. On a
/// pay-to-join event the place is held for them rather than given: they keep it by paying before
/// <see cref="PayBy"/>, and after that it passes to whoever is next.
/// </summary>
/// <remarks>
/// Not <see cref="WaitlistPromotedEvent"/>, which is a position on a team changing hands: that one
/// names the team and the position, and an event's own waiting list has neither.
/// </remarks>
public record EventWaitlistPromotedEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid UserId { get; init; }  // The person given the place
    public required Guid TargetEventId { get; init; }
    public required string EventName { get; init; }
    public required DateTime StartTime { get; init; }

    /// <summary>When a held place is released if it is still unpaid; null when the place is theirs.</summary>
    public DateTime? PayBy { get; init; }

    /// <summary>
    /// When the place was given. Someone can come off the same waiting list twice (an organiser
    /// moves them back onto it), so this, not the event alone, is what tells a repeat from a
    /// redelivery.
    /// </summary>
    public required DateTime PromotedAt { get; init; }
}
