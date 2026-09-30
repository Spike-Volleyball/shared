namespace Shared.Messaging.Contracts.Events.Clubs;

/// <summary>
/// A club admin asked to be emailed the link to the club's member import, which is easiest on a
/// computer: the phone app offers it where the web offers the upload. Notifications emails the
/// admin at the address on their own account, so the event names the account and never an address.
/// </summary>
public record MemberImportLinkEmailRequestedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>The admin who asked, and the only one emailed.</summary>
    public required Guid UserId { get; init; }

    public required Guid ClubId { get; init; }
    public required string ClubName { get; init; }
}
