namespace Shared.Messaging.Contracts.Events.Clubs;

/// <summary>
/// A website the club has not approved tried to show its registration form. Raised once per new
/// address, for the club's owners and admins to approve or block it in the club's settings.
/// </summary>
public record ClubEmbedRequestedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid ClubId { get; init; }
    public required string ClubName { get; init; }
    public required Guid EmbedSiteId { get; init; }

    /// <summary>The website as clubs keeps it: scheme://host[:port].</summary>
    public required string Origin { get; init; }

    /// <summary>The club's owners and admins when the request was filed.</summary>
    public required IReadOnlyList<Guid> RecipientUserIds { get; init; }
}
