namespace Shared.Messaging.Contracts.Events.Subscriptions;

/// <summary>
/// Published by payments-service when a membership becomes active, for the payer: its first
/// payment went through, or a renewal payment it had fallen behind on did (<see cref="Reason"/>).
/// One lifted from a suspension is told by <see cref="SubscriptionReinstatedEvent"/> instead.
/// </summary>
public record SubscriptionActiveEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>The payer.</summary>
    public required Guid UserId { get; init; }

    public required Guid SubscriptionId { get; init; }
    public required Guid ClubId { get; init; }

    /// <summary>Null when clubs could not be reached as the event was published.</summary>
    public string? ClubName { get; init; }

    public required string PlanName { get; init; }
    public required IReadOnlyList<SubscriptionCoveredPerson> Covered { get; init; }

    public required SubscriptionActiveReason Reason { get; init; }
}
