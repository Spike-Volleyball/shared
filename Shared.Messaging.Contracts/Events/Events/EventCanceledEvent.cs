namespace Shared.Messaging.Contracts.Events.Events;

public record EventCanceledEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid UserId { get; init; }  // Participant receiving notification
    public required Guid TargetEventId { get; init; }
    public required string EventName { get; init; }
    public required string CanceledByUserName { get; init; }
    public string? CancellationReason { get; init; }

    /// <summary>
    /// When the cancelled session was due to start. A series member holds several, and "Wed Night
    /// Volley cancelled" alone does not say which one.
    /// </summary>
    public DateTime? StartTime { get; init; }

    /// <summary>
    /// When the organiser cancelled: one value for every recipient of one cancellation. It is what
    /// tells two cancellations of one event apart once a restore sits between them, so the second
    /// is not taken for a duplicate of the first.
    /// </summary>
    public DateTime? CanceledAt { get; init; }
}
