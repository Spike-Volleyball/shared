namespace Shared.Messaging.Contracts.Events.Subscriptions;

/// <summary>Someone a membership covers, named as payments' copy of their profile had them.</summary>
public record SubscriptionCoveredPerson
{
    public required Guid UserId { get; init; }

    /// <summary>Null when payments had no name for them yet; the consumer's own copy of profiles may.</summary>
    public string? Name { get; init; }
}
