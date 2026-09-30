namespace Shared.Messaging.Contracts.Events.Subscriptions;

/// <summary>
/// Published by payments-service when someone applies for a membership plan that needs a club
/// admin's approval. It is for the club's owners, admins and treasurers, whom payments asks clubs
/// for as the application arrives; the payer is not told anything by it.
/// </summary>
public record SubscriptionApplicationReceivedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid SubscriptionId { get; init; }
    public required Guid ClubId { get; init; }

    /// <summary>Null when clubs could not be reached as the event was published.</summary>
    public string? ClubName { get; init; }

    public required string PlanName { get; init; }

    /// <summary>Who applied, and will pay once it is approved.</summary>
    public required Guid PayerUserId { get; init; }

    /// <summary>Null when payments had no name for the payer yet.</summary>
    public string? PayerName { get; init; }

    /// <summary>Who it would cover: the payer, children they manage, or both.</summary>
    public required IReadOnlyList<SubscriptionCoveredPerson> Covered { get; init; }

    /// <summary>The club's owners, admins and treasurers when the application arrived.</summary>
    public required IReadOnlyList<Guid> RecipientUserIds { get; init; }
}
