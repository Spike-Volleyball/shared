namespace Shared.Messaging.Contracts.Events.Messages;

public record MessageCreatedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Guid MessageId { get; init; }
    public Guid ChatId { get; init; }
    public Guid SenderId { get; init; }
    /// <summary>Platform-channel posts only: author chose to push this post. Default false — in-flight events during deploy stay silent.</summary>
    public bool NotifyPush { get; init; }
    /// <summary>The sender chose "send without sound": recipients are still pushed, without sound or vibration.</summary>
    public bool Silent { get; init; }
}
