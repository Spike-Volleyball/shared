using Shared.Messaging.Contracts.Events;

namespace Shared.Messaging.Contracts.Events.Auth;

/// <summary>
/// An inactive account a club made for someone not on Spike yet: a stub for an address it invited,
/// or a placeholder for a person it imported with no address at all. profiles makes their profile
/// from it, and the replicas follow.
/// </summary>
public record StubUserCreatedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid UserId { get; init; }
    public required string FirstName { get; init; }
    public string? LastName { get; init; }

    /// <summary>
    /// The address the club invited. Empty for a placeholder, whose account holds only a made-up
    /// address until its owner claims it with their own.
    /// </summary>
    public required string Email { get; init; }

    public string? PhoneNumber { get; init; }

    /// <summary>What the club gave for a placeholder; null for a stub, and when the club did not know.</summary>
    public DateTime? DateOfBirth { get; init; }

    public required Guid CreatedByUserId { get; init; }
    public required Guid CreatedForClubId { get; init; }
}
