namespace Shared.Messaging.Contracts.Events.Subscriptions;

/// <summary>
/// Published by payments-service when a membership's renewal payment fails, for the payer. The
/// membership carries on until <see cref="GraceEndsAt"/> and is suspended then unless paid. Paying
/// is done signed in, by adding a card on the membership itself, not through a recovery link.
/// </summary>
public record SubscriptionPaymentFailedEvent : INotificationEvent
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

    /// <summary>What each period costs, which the renewal asked for, in major units.</summary>
    public required decimal Amount { get; init; }

    public required string Currency { get; init; }
    public required DateTime GraceEndsAt { get; init; }
}
