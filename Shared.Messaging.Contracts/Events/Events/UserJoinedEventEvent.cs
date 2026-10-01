namespace Shared.Messaging.Contracts.Events.Events;

public record UserJoinedEventEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid UserId { get; init; }  // Organizer receiving notification
    public required Guid TargetEventId { get; init; }
    public required string EventName { get; init; }
    public required Guid JoinedUserId { get; init; }
    public required string JoinedUserName { get; init; }

    /// <summary>
    /// How many people hold a seat once this change lands, and the cap if the event has one —
    /// counted the way the capacity gate counts, so the number a notification shows and the number
    /// that turns someone away can never disagree. Optional: a publisher that has not been taught
    /// to send them leaves the count out of the notification rather than showing a wrong one.
    /// </summary>
    public int? AttendingCount { get; init; }
    public int? Capacity { get; init; }

    /// <summary>
    /// What was paid for this place, when paying is what secured it — an event that charges to
    /// join announces nobody until the money has landed. Optional: a publisher that does not send
    /// it, and a place nobody paid for, leave the payment out of the notification.
    /// </summary>
    public decimal? PaidAmount { get; init; }
    public string? PaidCurrency { get; init; }
}
