namespace Shared.Messaging.Contracts.Events.Registrations;

/// <summary>
/// A registration reached a club, told to whoever filed it: the applicant, or the guardian who
/// registered a child. Its counterpart <see cref="ClubRegistrationSubmittedEvent"/> goes to the
/// club's admins.
/// <para>
/// It names the recipient's address because the account may be minutes old: the session that
/// filed the registration carries the address, while a profile replica may not have it yet.
/// </para>
/// </summary>
public record ClubRegistrationReceivedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid RegistrationId { get; init; }
    public required Guid ClubId { get; init; }
    public required string ClubName { get; init; }

    /// <summary>Who filed the registration and is told it arrived.</summary>
    public required Guid RecipientUserId { get; init; }

    /// <summary>The recipient's address from their session, normalised; empty when it had none.</summary>
    public required string RecipientEmail { get; init; }

    /// <summary>Who was registered: the recipient themselves, or the child they registered.</summary>
    public required Guid ApplicantUserId { get; init; }
    public required string ApplicantName { get; init; }
}
