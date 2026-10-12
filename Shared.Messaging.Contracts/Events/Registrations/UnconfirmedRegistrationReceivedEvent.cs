namespace Shared.Messaging.Contracts.Events.Registrations;

/// <summary>
/// Someone applied to a club without signing in, told to each of the club's admins: the application
/// is theirs to decide at once, before the applicant has confirmed anything. One per applicant, as
/// <see cref="ClubRegistrationSubmittedEvent"/> is one per registration.
/// <para>
/// It names the applicant and the contact rather than an account, because there is none yet, and
/// carries the unconfirmed registration's id for the admin queue to open.
/// </para>
/// </summary>
public record UnconfirmedRegistrationReceivedEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>The admin told.</summary>
    public required Guid UserId { get; init; }

    public required Guid ClubId { get; init; }
    public required string ClubName { get; init; }
    public required Guid UnconfirmedRegistrationId { get; init; }
    public required string ApplicantName { get; init; }

    /// <summary>The adult who filled the form in, who is the applicant when they applied themselves.</summary>
    public required string ContactName { get; init; }

    public required bool IsChild { get; init; }
}
