namespace Shared.Messaging.Contracts.Events.Clubs;

/// <summary>
/// Published by clubs-service whenever a club's settings are saved. It says only which club: a
/// service keeping a copy of anything read from them drops it and reads again when next it needs
/// them. events keeps the club's labels, whose decline-note rule must apply at once both ways.
/// </summary>
public record ClubSettingsChangedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid ClubId { get; init; }
}
