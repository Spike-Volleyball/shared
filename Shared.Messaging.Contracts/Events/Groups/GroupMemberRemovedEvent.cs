namespace Shared.Messaging.Contracts.Events.Groups;

public record GroupMemberRemovedEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid UserId { get; init; }
    public required Guid ClubId { get; init; }
    public required string ClubName { get; init; }
    public required Guid GroupId { get; init; }
    public required string GroupName { get; init; }

    /// <summary>
    /// The club stands in as the actor on the notification this event raises: nobody is named
    /// as having done the removal, so the club's own logo is what the row shows.
    /// </summary>
    public string? ClubLogoUrl { get; init; }

    /// <summary>
    /// True when the row went because the club membership behind it ended - the member left the
    /// club or was removed from it - rather than because someone took them out of this group.
    /// Leaving the club is the news, so nobody is told about each of its groups; anything that
    /// cleans up after a removal treats this one like any other.
    /// </summary>
    public bool IsClubDeparture { get; init; }
}
