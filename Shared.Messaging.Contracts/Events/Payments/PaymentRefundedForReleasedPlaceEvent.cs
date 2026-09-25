namespace Shared.Messaging.Contracts.Events.Payments;

/// <summary>
/// payments-service refunded a payment that went through for a place already let go
/// (<see cref="PaymentArrivedForReleasedPlaceEvent"/>). Published once the provider has taken the
/// refund, so the payer can be told the money is on its way back, and why.
/// </summary>
public record PaymentRefundedForReleasedPlaceEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>Who paid, and so whose card the money goes back to.</summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// A payment is refunded for a released place once, so this, not the event, is what tells a
    /// redelivery from a second refund.
    /// </summary>
    public required Guid PaymentId { get; init; }

    public required Guid TargetEventId { get; init; }
    public required string EventName { get; init; }

    /// <summary>What went back, in major units.</summary>
    public required decimal Amount { get; init; }

    public required string Currency { get; init; }
}
