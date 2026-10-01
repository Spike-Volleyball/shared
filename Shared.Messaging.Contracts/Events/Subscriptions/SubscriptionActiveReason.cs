namespace Shared.Messaging.Contracts.Events.Subscriptions;

/// <summary>Why a membership became active, so the copy can say so.</summary>
public enum SubscriptionActiveReason
{
    /// <summary>Its first payment went through, and cover begins.</summary>
    Started = 0,

    /// <summary>A renewal payment it had fallen behind on went through before it was suspended.</summary>
    PaymentRecovered = 1
}
