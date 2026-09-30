namespace Shared.Messaging.Contracts.Events.Subscriptions;

/// <summary>A request to move an existing membership from one plan to another that needs a club admin's approval.</summary>
public record SubscriptionPlanChange
{
    public required Guid FromPlanId { get; init; }
    public required string FromPlanName { get; init; }
    public required Guid ToPlanId { get; init; }
    public required string ToPlanName { get; init; }

    /// <summary>When the move takes effect, as asked for or as approved: the end of the current period. Null for a rejected one.</summary>
    public DateTime? EffectiveAt { get; init; }
}
