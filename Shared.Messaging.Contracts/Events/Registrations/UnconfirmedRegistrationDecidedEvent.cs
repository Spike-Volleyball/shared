namespace Shared.Messaging.Contracts.Events.Registrations;

/// <summary>
/// A club's admins decided an application nobody has confirmed yet, and the contact is told by email.
/// The decision waits on the record and is applied when the link is opened and signed in with.
/// <para>
/// It names the contact's address for the reason <see cref="UnconfirmedRegistrationSubmittedEvent"/>
/// does, and carries a token because every decision issues a fresh link; the ones emailed before keep
/// working. <see cref="NewStatus"/> is a string, as in <see cref="RegistrationStatusChangedEvent"/>.
/// </para>
/// </summary>
public record UnconfirmedRegistrationDecidedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid UnconfirmedRegistrationId { get; init; }
    public required Guid SubmissionId { get; init; }
    public required Guid ClubId { get; init; }
    public required string ClubName { get; init; }
    public string? ClubLogoUrl { get; init; }

    /// <summary>Normalised: trimmed and in lower case.</summary>
    public required string ContactEmail { get; init; }

    public required string ContactFirstName { get; init; }

    /// <summary>Whom the decision is about, who is not always the contact.</summary>
    public required string ApplicantName { get; init; }

    public required bool IsChild { get; init; }
    public required string NewStatus { get; init; }

    /// <summary>The note the admins wrote for the applicant; never their private one.</summary>
    public string? PublicNote { get; init; }

    public required string ConfirmToken { get; init; }
    public required DateTime ExpiresAt { get; init; }
}
