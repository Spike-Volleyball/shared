namespace Shared.Messaging.Contracts.Events.Clubs;

/// <summary>
/// A club imported a child from its old system, and asks the guardian it named to confirm them. The
/// child joins only once the guardian confirms, from their own family: a club never puts a child
/// in someone's family by itself.
/// <para>
/// It names the guardian's address because they may not be on Spike at all. The token is the
/// confirmation link's; clubs keeps only its hash, so this is the one place it travels.
/// </para>
/// </summary>
public record ChildImportCreatedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid ChildImportId { get; init; }
    public required Guid ClubId { get; init; }
    public required string ClubName { get; init; }
    public required string ChildFirstName { get; init; }
    public required string ChildLastName { get; init; }

    /// <summary>Normalised: trimmed and in lower case.</summary>
    public required string GuardianEmail { get; init; }

    /// <summary>As the club's list gave it; null when it gave none.</summary>
    public string? GuardianName { get; init; }

    public required string ConfirmToken { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required Guid ImportedByUserId { get; init; }
}
