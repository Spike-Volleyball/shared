namespace Shared.Messaging.Contracts.Events.Events;

public class EventHostInvitationCreatedEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Guid UserId => InvitedUserId;
    public Guid InvitedUserId { get; set; }
    public Guid InvitationEventId { get; set; }

    public string EventName { get; set; }
    public string EventLocationName { get; set; }
    public DateTime EventDateTime { get; set; }

    /// <summary>
    /// The IANA zone of the place the event is played, so its time reads as it does on that place's
    /// clocks wherever the recipient is. Null when it has no pinned venue, or from a publisher that
    /// predates the field: the time is then read in the notification's default zone.
    /// </summary>
    public string? TimeZone { get; set; }

    public Guid? InvitedByUserId { get; set; }
    public string? InviterName { get; set; }
    public string? InviterPhotoUrl { get; set; }

    /// <summary>
    /// When the invitation was made, for an invitation that can follow an earlier one to host the
    /// same event: someone moved to the hosts, back to the participants and to the hosts again is
    /// asked twice. It keys the notice on the action as well as the event, so a repeat is told from
    /// a redelivery. Null from a publisher whose invitation is the first, which stays keyed on the
    /// event alone.
    /// </summary>
    public DateTime? InvitedAt { get; init; }
}
