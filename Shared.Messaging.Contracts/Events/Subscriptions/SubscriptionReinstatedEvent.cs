namespace Shared.Messaging.Contracts.Events.Subscriptions;

/// <summary>
/// Published by payments-service when a suspended membership is active again, for the payer:
/// a club admin lifted the suspension, or the payment it waited on went through.
/// </summary>
public record SubscriptionReinstatedEvent : INotificationEvent
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

    /// <summary>Why it had been suspended, which says what lifted it: an admin, or the payment.</summary>
    public required SubscriptionSuspensionReason SuspendedFor { get; init; }
}
