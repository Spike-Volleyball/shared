namespace Shared.Messaging.Contracts.Events.Clubs;

/// <summary>
/// Published by clubs-service once whenever a membership starts, however it started: a registration
/// accepted, an invitation or invite link accepted, a former member back, a child their guardian
/// confirmed. What a membership brings (its chat, and its session invitations when an organiser
/// let them in) hangs off this, the counterpart of <see cref="UserRemovedFromClubEvent"/>.
/// <see cref="ClubMemberJoinedEvent"/> is the admins' notice: one copy per admin, and only on the
/// paths that tell them.
/// </summary>
public record UserJoinedClubEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid UserId { get; init; }
    public required Guid ClubId { get; init; }

    /// <summary>
    /// The organiser who accepted the registration this membership started from, and who is
    /// therefore there to say which sessions the new member is invited to. Null when nobody
    /// decided the join: an invitation or a link taken up, a child their guardian confirmed.
    /// </summary>
    public Guid? AcceptedByUserId { get; init; }
}
