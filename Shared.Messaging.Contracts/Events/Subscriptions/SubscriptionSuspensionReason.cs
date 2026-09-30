namespace Shared.Messaging.Contracts.Events.Subscriptions;

/// <summary>Why a membership was suspended, which decides what lifts it.</summary>
public enum SubscriptionSuspensionReason
{
    /// <summary>A club admin suspended it, and only an admin lifts it.</summary>
    Admin = 0,

    /// <summary>A renewal payment failed and the grace period ran out; paying what is owed lifts it.</summary>
    PaymentLapsed = 1
}
