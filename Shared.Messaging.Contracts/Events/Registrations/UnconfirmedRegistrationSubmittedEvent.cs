namespace Shared.Messaging.Contracts.Events.Registrations;

/// <summary>
/// Someone applied to a club without signing in, and is sent the link that turns what they typed into
/// registrations on their own account. The club holds the submission meanwhile, as its own record and
/// belonging to no account.
/// <para>
/// It names the contact's address because nobody is signed in to be told: the address is the one typed
/// on the form, and nothing has matched it to an account. The token is the link's; clubs keeps only its
/// hash, so this is the one place it travels.
/// </para>
/// </summary>
public record UnconfirmedRegistrationSubmittedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid SubmissionId { get; init; }
    public required Guid ClubId { get; init; }
    public required string ClubName { get; init; }

    /// <summary>Normalised: trimmed and in lower case.</summary>
    public required string ContactEmail { get; init; }

    public required string ContactFirstName { get; init; }

    /// <summary>
    /// Who applied, by full name, the contact first when they applied themselves: the email says whom
    /// the link is for without the answers travelling.
    /// </summary>
    public required IReadOnlyList<string> ApplicantNames { get; init; }

    /// <summary>Whether a child is among them, whom a guardian confirms from their own family.</summary>
    public required bool IncludesChild { get; init; }

    public required string ConfirmToken { get; init; }
    public required DateTime ExpiresAt { get; init; }
}
