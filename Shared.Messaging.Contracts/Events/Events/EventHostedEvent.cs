namespace Shared.Messaging.Contracts.Events.Events;

/// <summary>
/// An event that took place, and who organised it: published once it has ended, and again,
/// <see cref="HostedState.Withdrawn"/>, if it is cancelled or deleted afterwards. Each copy replaces the
/// last; a consumer keeps the copy with the latest <see cref="SnapshotAt"/>.
/// </summary>
public record EventHostedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid HostedEventId { get; init; }
    public required Guid OrganiserUserId { get; init; }
    public required string Name { get; init; }
    public Guid? ClubId { get; init; }
    public required HostedState State { get; init; }

    /// When it ended.
    public required DateTime EndedAt { get; init; }

    public required DateTime SnapshotAt { get; init; }
}

public enum HostedState
{
    Held,
    Withdrawn
}
