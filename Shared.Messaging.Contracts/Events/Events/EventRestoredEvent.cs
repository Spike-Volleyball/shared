namespace Shared.Messaging.Contracts.Events.Events;

/// <summary>
/// A cancelled event is back on. It goes to everyone the cancellation reached, so nobody who was
/// told it was off is left believing it still is.
/// </summary>
public record EventRestoredEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid UserId { get; init; }  // Participant receiving notification
    public required Guid TargetEventId { get; init; }
    public required string EventName { get; init; }
    public required string RestoredByUserName { get; init; }

    /// <summary>
    /// When the restored session starts. A series member holds several under one name, and "Wed Night
    /// Volley is back on" alone does not say which one.
    /// </summary>
    public DateTime? StartTime { get; init; }

    /// <summary>
    /// The IANA zone of the place the event is played, so its time reads as it does on that place's
    /// clocks wherever the recipient is. Null when it has no pinned venue, or from a publisher that
    /// predates the field: the time is then read in the notification's default zone.
    /// </summary>
    public string? TimeZone { get; init; }

    /// <summary>
    /// When the organiser restored it: one value for every recipient of one restore, so a second
    /// restore of the same event is not taken for a duplicate of the first.
    /// </summary>
    public DateTime? RestoredAt { get; init; }
}
