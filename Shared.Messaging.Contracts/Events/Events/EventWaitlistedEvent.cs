namespace Shared.Messaging.Contracts.Events.Events;

/// <summary>
/// Someone was put on an event's waiting list by an action of an organiser's rather than by asking
/// for a place: a host moved back to the participants of an event that is already full. Joining a
/// full event is answered to the person who did it, so this is for the cases where they did not.
/// One message stands for every date one action put them on the list for, headlined by the earliest.
/// </summary>
public record EventWaitlistedEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid UserId { get; init; }  // The person put on the waiting list

    /// <summary>The earliest of the dates they are waiting for.</summary>
    public required Guid TargetEventId { get; init; }

    /// <summary>Counts the dates when there is more than one, under the series' name if they share one.</summary>
    public required string EventName { get; init; }

    /// <summary>When the headlined date starts.</summary>
    public required DateTime StartTime { get; init; }

    /// <summary>
    /// When they were put on the list. Someone can be put on the same event's list more than once
    /// (moved to the hosts and back), so this, not the event alone, is what tells a repeat from a
    /// redelivery.
    /// </summary>
    public required DateTime WaitlistedAt { get; init; }
}
