namespace Shared.Messaging.Contracts.Events.Subscriptions;

/// <summary>
/// Published by payments-service when one person's cover on a membership is set to end while it
/// carries on for the others, for the payer. Today that is a child whose payer's guardian access
/// was revoked: the cover already paid for runs to the end of the period.
/// </summary>
public record SubscriptionCoverEndingEvent : INotificationEvent
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

    /// <summary>Whose cover ends.</summary>
    public required SubscriptionCoveredPerson Leaving { get; init; }

    public required DateTime LeavesAt { get; init; }
}
