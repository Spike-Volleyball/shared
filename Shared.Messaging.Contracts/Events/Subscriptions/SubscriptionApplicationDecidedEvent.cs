namespace Shared.Messaging.Contracts.Events.Subscriptions;

/// <summary>
/// Published by payments-service when a club admin approves or rejects an application to join a
/// membership plan, or a request to move a membership to another (<see cref="PlanChange"/>), for
/// the payer. An approved application is not a membership yet: it starts once paid, and lapses
/// unpaid at <see cref="PayBy"/>.
/// </summary>
public record SubscriptionApplicationDecidedEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>The payer, who applied.</summary>
    public required Guid UserId { get; init; }

    public required Guid SubscriptionId { get; init; }
    public required Guid ClubId { get; init; }

    /// <summary>Null when clubs could not be reached as the event was published.</summary>
    public string? ClubName { get; init; }

    public required string PlanName { get; init; }
    public required IReadOnlyList<SubscriptionCoveredPerson> Covered { get; init; }

    public required bool Approved { get; init; }

    /// <summary>When an approved application lapses unpaid; null for a rejected one.</summary>
    public DateTime? PayBy { get; init; }

    /// <summary>The admin's reason for a rejection, when they gave one.</summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Set for a decision on a move to another plan rather than on joining one. <see cref="PlanName"/>
    /// is then the plan asked for, and <see cref="PayBy"/> stays null: the move is billed at the renewal.
    /// </summary>
    public SubscriptionPlanChange? PlanChange { get; init; }
}
