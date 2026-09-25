namespace Shared.Messaging.Contracts.Events.Events;

/// <summary>
/// Who turned up to one event, whole: published whenever the organiser marks attendance, and again when
/// the event is cancelled, deleted or restored. Each copy replaces the last; a consumer keeps the copy
/// with the latest <see cref="SnapshotAt"/>. A withdrawn copy names the event and nobody else.
/// </summary>
/// <remarks>Carries user ids only; a person's name is resolved by whoever renders it.</remarks>
public record AttendanceChangedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// The event whose attendance this is.
    public required Guid SessionId { get; init; }

    public Guid? ClubId { get; init; }

    /// The club's name as it stands when the snapshot is taken; absent when the event has no club.
    public string? ClubName { get; init; }

    public required string Name { get; init; }
    public required AttendanceState State { get; init; }

    /// When it started and ended in UTC.
    public required DateTime StartsAt { get; init; }
    public required DateTime EndsAt { get; init; }

    /// The same moments on the clock where it was played, so "before 8am" means 8am there.
    public required DateTime StartsAtLocal { get; init; }
    public required DateTime EndsAtLocal { get; init; }

    /// Played on sand.
    public required bool Beach { get; init; }

    public Guid? VenueId { get; init; }

    /// The players marked as attended. Empty when withdrawn.
    public required IReadOnlyList<Guid> AttendedUserIds { get; init; }

    public required DateTime SnapshotAt { get; init; }
}

public enum AttendanceState
{
    Held,
    Withdrawn
}
