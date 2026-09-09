using Shared.Enums;

namespace Shared.Messaging.Contracts.Events.Payments;

/// <summary>
/// Canonical signal for any change to a Payment's status. Published by payments-service through
/// the outbox after a durable status transition. Replaces the per-transition events
/// (PaymentCompletedEvent, PaymentFailedEvent, PaymentCancelledEvent) which remain only for
/// backwards-compat during migration.
///
/// Consumers must apply this event idempotently. <see cref="Version"/> orders events WITHIN one
/// payment and restarts at 1 for the next payment on the same target, so it cannot order events
/// across payments — a projection that compares it blindly discards a whole later payment as
/// stale. Order within a payment by Version, between payments by <see cref="ChangedAt"/>.
/// </summary>
public record PaymentStatusChangedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid PaymentId { get; init; }
    public required string TargetType { get; init; }
    public required Guid TargetId { get; init; }

    public required PaymentStatus OldStatus { get; init; }
    public required PaymentStatus NewStatus { get; init; }

    /// <summary>
    /// Monotonically-increasing version of the canonical Payment row — PER ROW. A new payment
    /// for the same target starts again at 1, so this only orders events sharing a PaymentId.
    /// </summary>
    public required long Version { get; init; }

    public required PaymentMethodType PaymentMethod { get; init; }
    public required PaymentProviderType Provider { get; init; }

    public Guid? ChangedByUserId { get; init; }
    public string? Reason { get; init; }
    public required DateTime ChangedAt { get; init; }
}
