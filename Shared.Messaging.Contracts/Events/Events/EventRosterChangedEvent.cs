namespace Shared.Messaging.Contracts.Events.Events;

/// <summary>
/// Somebody's place on an event's roster moved: a participant or host row was added, removed or
/// had its status changed. Carries who, not what — the audience of the event's chat is whatever
/// events-service's chat-members rule says now, so a consumer re-reads that rather than trusting
/// a delta that a reordered or redelivered message could make wrong.
/// </summary>
public record EventRosterChangedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid TargetEventId { get; init; }
    public required IReadOnlyList<Guid> UserIds { get; init; }
}
