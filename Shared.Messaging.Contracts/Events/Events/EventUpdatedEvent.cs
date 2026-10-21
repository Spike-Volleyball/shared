namespace Shared.Messaging.Contracts.Events.Events;

public record EventUpdatedEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid UserId { get; init; }  // Participant receiving notification
    public required Guid TargetEventId { get; init; }
    public required string EventName { get; init; }
    public required List<string> ChangedFields { get; init; }
    public required string UpdatedByUserName { get; init; }

    /// <summary>
    /// What the fields named in <see cref="ChangedFields"/> now say. Optional: a body that cannot
    /// show the new time simply says which fields moved, which is what it did before these existed
    /// and is better than guessing.
    /// </summary>
    public DateTime? NewStartTime { get; init; }

    /// <summary>
    /// The IANA zone of the place the event is played, so its time reads as it does on that place's
    /// clocks wherever the recipient is. Null when it has no pinned venue, or from a publisher that
    /// predates the field: the time is then read in the notification's default zone.
    /// </summary>
    public string? TimeZone { get; init; }

    public string? NewLocationName { get; init; }

    /// <summary>
    /// When the edit happened. Carries the identity the contract lacked: EventId and Timestamp take
    /// per-message defaults, so they differ between the two recipients of ONE edit and cannot key a
    /// dedupe. With this, EventUpdatedConsumer can finally stop notifying a guardian-who-plays twice.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }
}
