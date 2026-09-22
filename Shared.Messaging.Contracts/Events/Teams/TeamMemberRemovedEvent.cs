namespace Shared.Messaging.Contracts.Events.Teams;

public record TeamMemberRemovedEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid UserId { get; init; }
    public required Guid ClubId { get; init; }
    public required string ClubName { get; init; }
    public required Guid TeamId { get; init; }
    public required string TeamName { get; init; }

    /// <summary>
    /// True when the row went because the club membership behind it ended - the member left the
    /// club or was removed from it - rather than because someone took them off this team.
    /// Leaving the club is the news, so nobody is told about each of its teams; anything that
    /// cleans up after a removal treats this one like any other.
    /// </summary>
    public bool IsClubDeparture { get; init; }
}
