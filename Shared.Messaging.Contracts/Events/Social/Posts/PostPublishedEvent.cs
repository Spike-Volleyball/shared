namespace Shared.Messaging.Contracts.Events.Social.Posts;

/// <summary>
/// One post and how many photos it carries, whole: published when a post is published or edited, and
/// once more, <see cref="PostState.Withdrawn"/>, when it is deleted or hidden. Each copy replaces the
/// last; a consumer keeps the copy with the latest <see cref="SnapshotAt"/>.
/// </summary>
public record PostPublishedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid PostId { get; init; }
    public required Guid AuthorId { get; init; }
    public required PostState State { get; init; }
    public required int PhotoCount { get; init; }
    public required DateTime PublishedAt { get; init; }
    public required DateTime SnapshotAt { get; init; }
}

public enum PostState
{
    Published,
    Withdrawn
}
