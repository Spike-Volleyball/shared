namespace Shared.Messaging.Contracts.Events.Clubs;

/// <summary>
/// Published by clubs-service once whenever a membership starts, however it started: a registration
/// accepted, an invitation or invite link accepted, a former member back, a child their guardian
/// confirmed. What a membership brings (the club's session invitations, its chat) hangs off this,
/// the counterpart of <see cref="UserRemovedFromClubEvent"/>. <see cref="ClubMemberJoinedEvent"/>
/// is the admins' notice: one copy per admin, and only on the paths that tell them.
/// </summary>
public record UserJoinedClubEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid UserId { get; init; }
    public required Guid ClubId { get; init; }
}
