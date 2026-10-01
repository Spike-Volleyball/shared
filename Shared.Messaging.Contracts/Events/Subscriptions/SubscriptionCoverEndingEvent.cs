namespace Shared.Messaging.Contracts.Events.Subscriptions;

/// <summary>
/// Published by payments-service when one person's cover on a membership ends while it carries on
/// for the others, for the payer: at the end of the period already paid for when the payer's
/// guardian access to them was revoked, or at once when they leave the club (<see cref="Cause"/>).
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

    /// <summary>When their cover ends: the end of the period, or the moment they left the club.</summary>
    public required DateTime LeavesAt { get; init; }

    public SubscriptionCoverEndCause Cause { get; init; }
}
