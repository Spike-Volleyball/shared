namespace Shared.Enums;

/// <summary>
/// How the payment was satisfied. Drives revenue reporting:
/// Card/CardPresent/Cash count as collected; Subscription is covered separately;
/// ManualAdjustment displays as "Other" and is excluded from collected totals.
/// </summary>
public enum PaymentMethodType
{
    Card = 0,
    Cash = 1,
    Subscription = 2,
    ManualAdjustment = 3,

    /// <summary>
    /// A card tapped in person, through Stripe Terminal. Distinct from Card for two reasons:
    /// in-person takings are tellable apart from online ones in reporting, and the Payments
    /// unique index is (TargetType, TargetId, PaymentMethod) filtered to Pending — sharing
    /// Card would mean an abandoned online checkout blocks the organiser taking the same
    /// player's card at the door.
    /// </summary>
    CardPresent = 4
}
