
namespace Shared.Messaging.Contracts.Events.Events;

public class EventInvitationCreatedEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Guid UserId => InvitedUserId;
    public Guid InvitedUserId { get; set; }
    public Guid InvitationEventId { get; set; }

    public string EventName { get; set; }
    public string EventLocationName { get; set; }
    public DateTime EventDateTime { get; set; }

    public Guid? InvitedByUserId { get; set; }
    public string? InviterName { get; set; }
    public string? InviterPhotoUrl { get; set; }

    /// <summary>
    /// When the invitation was made, for an invitation that can follow an earlier one to the same
    /// event: a host moved back to the participants is invited afresh to dates they were once
    /// invited to as a player. It keys the notice on the action as well as the event, so a repeat
    /// is told from a redelivery. Null from a publisher whose invitation is the first, which stays
    /// keyed on the event alone.
    /// </summary>
    public DateTime? InvitedAt { get; init; }
}
