namespace Shared.Messaging.Contracts.Events.Subscriptions;

/// <summary>Why one person's cover on a membership ends while it carries on for the others.</summary>
public enum SubscriptionCoverEndCause
{
    /// <summary>The payer's guardian access to them was revoked; the cover paid for runs to the end of the period.</summary>
    GuardianAccessRevoked = 0,

    /// <summary>They left the club, or were removed from it; their cover ends at once.</summary>
    RemovedFromClub = 1
}
