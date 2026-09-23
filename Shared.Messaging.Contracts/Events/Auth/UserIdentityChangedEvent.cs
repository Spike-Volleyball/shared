using Shared.Enums;

namespace Shared.Messaging.Contracts.Events.Auth;

/// <summary>
/// auth owns date of birth and country code; profiles and the six services that replicate
/// <c>UserProfile</c> hold copies. Published whenever the owner's value changes so those copies
/// can be fed — a write applied only in profiles reaches the profile row and none of the replicas.
/// </summary>
public record UserIdentityChangedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid UserId { get; init; }
    public required DateTime DateOfBirth { get; init; }
    public string? CountryCode { get; init; }
    public required AgeTier AgeTier { get; init; }

    /// <summary>
    /// auth's verification flag, which the copies downstream gate receipts on. Null when the
    /// publisher does not speak for it, and the copies keep what they hold.
    /// </summary>
    public bool? IsEmailVerified { get; init; }
}
