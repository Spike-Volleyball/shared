namespace Shared.Messaging.Contracts.Events.Events;

/// <summary>
/// A card tapped at the door was declined. Stripe sends a receipt only for a payment that went
/// through, so this is the payer's record of the one that did not: nothing was taken.
/// </summary>
public record EventPaymentDeclinedEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>The player whose card was declined.</summary>
    public required Guid UserId { get; init; }

    public required Guid TargetEventId { get; init; }
    public required string EventName { get; init; }

    /// <summary>What the reader asked for, in major units.</summary>
    public required decimal Amount { get; init; }

    public required string Currency { get; init; }
}
