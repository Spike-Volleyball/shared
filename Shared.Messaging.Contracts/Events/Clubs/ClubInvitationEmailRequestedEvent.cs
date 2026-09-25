namespace Shared.Messaging.Contracts.Events.Clubs;

/// <summary>
/// A club invited someone by the email address an admin typed. Notifications emails that address
/// the invitation link, naming the club and who invited them.
/// <para>
/// It carries the address and never an account. Clubs does not look the address up, and the email
/// goes out the same whether or not anyone on Spike holds it, so inviting somebody cannot tell an
/// admin who is on Spike (SPI-6442). The counterpart for an invitation to an account is
/// <see cref="ClubInvitationCreatedEvent"/>, which reaches them in the app.
/// </para>
/// </summary>
public record ClubInvitationEmailRequestedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid InvitationId { get; init; }
    public required Guid ClubId { get; init; }
    public required string ClubName { get; init; }
    public required string InvitedByUserName { get; init; }

    /// <summary>As the admin typed it, trimmed and in lower case.</summary>
    public required string TargetEmail { get; init; }

    /// <summary>The invitation link's token: the link is built from it as the web's Invite dialog builds its own.</summary>
    public required string InvitationToken { get; init; }
}
