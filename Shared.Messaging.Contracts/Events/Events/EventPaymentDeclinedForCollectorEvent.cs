namespace Shared.Messaging.Contracts.Events.Events;

/// <summary>
/// A card declined at the door, addressed to the organiser who ran the reader. Their sheet shows
/// the decline while it is open; this reaches them when it no longer is — they left the app while
/// the payment was still being approved (Apple Tap to Pay 5.12).
/// </summary>
public record EventPaymentDeclinedForCollectorEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>The organiser who ran the reader.</summary>
    public required Guid UserId { get; init; }

    public required Guid TargetEventId { get; init; }
    public required string EventName { get; init; }

    /// <summary>The participant whose card was declined, so an open sheet for them can stay quiet.</summary>
    public required Guid ParticipantId { get; init; }

    public required string PlayerName { get; init; }

    /// <summary>What the reader asked for, in major units.</summary>
    public required decimal Amount { get; init; }

    public required string Currency { get; init; }
}
