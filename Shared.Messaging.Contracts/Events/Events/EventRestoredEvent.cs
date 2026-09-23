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
}
