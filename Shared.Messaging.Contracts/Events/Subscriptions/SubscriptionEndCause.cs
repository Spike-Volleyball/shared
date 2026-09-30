namespace Shared.Messaging.Contracts.Events.Subscriptions;

/// <summary>Why a membership stopped renewing or ended, so the copy can say so.</summary>
public enum SubscriptionEndCause
{
    /// <summary>The payer cancelled it.</summary>
    MemberCancelled = 0,

    /// <summary>It was never paid for: an approved application, or a checkout, left unpaid.</summary>
    NotPaidInTime = 1,

    /// <summary>Renewal payments kept failing until Stripe stopped trying.</summary>
    PaymentLapsed = 2,

    /// <summary>The last person it covered left the club, or was removed from it.</summary>
    RemovedFromClub = 3,

    /// <summary>The payer's guardian access to the child it covered was revoked.</summary>
    GuardianAccessRevoked = 4,

    /// <summary>It was ended in Stripe rather than through Spike, so the reason is not known.</summary>
    Other = 5
}
