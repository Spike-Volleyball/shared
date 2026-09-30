namespace Shared.Messaging.Contracts.Events.Subscriptions;

/// <summary>
/// Published by payments-service ahead of a renewal, for the payer, as UK subscription rules ask:
/// 21 days before each renewal of an annual plan, and once in every six months of a monthly or
/// quarterly one. What it states is what the renewal will charge, a booked move to another plan
/// included.
/// </summary>
public record SubscriptionRenewalReminderEvent : INotificationEvent
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

    public required DateTime RenewsAt { get; init; }

    /// <summary>What the renewal will charge, in major units.</summary>
    public required decimal Amount { get; init; }

    public required string Currency { get; init; }
    public required SubscriptionBillingInterval Interval { get; init; }
}
