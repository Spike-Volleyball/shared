namespace Shared.Messaging.Contracts.Events.Events;

public class EventRespondReminderEvent : INotificationEvent
{
    /// <summary>
    /// What a reminder says when nobody wrote its words; the notification adds the event's date
    /// after it. One constant, because three places have to agree on it: the notification that
    /// falls back to it, the club's settings that offer it to edit, and the services that treat
    /// a message equal to it as none.
    /// </summary>
    public const string DefaultMessage = "You haven't answered yet";

    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Guid UserId => RecipientUserId;

    public Guid RecipientUserId { get; set; }
    public Guid TargetEventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventStartTime { get; set; }

    /// <summary>
    /// The words that stand in for <see cref="DefaultMessage"/>, if anyone wrote any: what the
    /// organiser typed when they sent the nudge, or the club's own words for its automatic one.
    /// Optional: a send need not carry one.
    /// </summary>
    public string? ReminderMessage { get; set; }
}
