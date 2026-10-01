namespace Shared.Messaging.Contracts.Events.Subscriptions;

/// <summary>
/// Published by payments-service when a membership stops renewing or ends, for the payer: when it
/// is cancelled to run out at the end of the period already paid for (<see cref="EndsAt"/> set),
/// and when it ends at once (<see cref="EndsAt"/> null). A membership cancelled to run out is not
/// announced again when that period ends, an application that ends before anyone decided on it is
/// not announced, and nothing is sent to a deleted account.
/// </summary>
public record SubscriptionEndedEvent : INotificationEvent
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

    public required SubscriptionEndCause Cause { get; init; }

    /// <summary>When cover runs out, for a membership seeing out the period paid for; null when it ended at once.</summary>
    public DateTime? EndsAt { get; init; }

    /// <summary>The cooling-off refund, in major units; null when nothing was refunded.</summary>
    public decimal? RefundAmount { get; init; }

    public required string Currency { get; init; }
}
