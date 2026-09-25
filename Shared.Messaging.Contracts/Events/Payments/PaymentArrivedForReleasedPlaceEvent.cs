namespace Shared.Messaging.Contracts.Events.Payments;

/// <summary>
/// A payment went through for a place that its target no longer holds: the place was let go while
/// the payment could still land. The service that held the place cannot give the money anywhere,
/// so it names the payment and payments-service gives it back.
/// </summary>
/// <remarks>
/// Only the payment and its target. By the time the payment lands the place is gone, and with it
/// whatever the publisher knew about whose it was; payments-service refunds what its own row says
/// was paid, to whoever paid it, never an amount a message names.
/// </remarks>
public record PaymentArrivedForReleasedPlaceEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid PaymentId { get; init; }
    public required string TargetType { get; init; }
    public required Guid TargetId { get; init; }
}
