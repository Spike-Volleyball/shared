namespace Shared.Messaging.Contracts.Events.Coaching;

/// <summary>
/// One piece of feedback a coach shared with a player, and the badge it carries, whole: published when
/// feedback is shared, with or without praise, and every time its badge changes, and once more,
/// <see cref="PraiseState.Withdrawn"/>, when the feedback is unshared or deleted. Each copy replaces the
/// last; a consumer keeps the copy with the latest <see cref="SnapshotAt"/>.
/// </summary>
/// <remarks>Unlike <see cref="FeedbackSharedEvent"/>, it notifies nobody, so history can be republished.</remarks>
public record PraiseGivenEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid FeedbackId { get; init; }
    public required Guid CoachUserId { get; init; }
    public required Guid PlayerUserId { get; init; }
    public required PraiseState State { get; init; }

    /// The badge the coach chose — the BadgeType's name, e.g. "Star", "Hustle". Absent when the feedback
    /// carries no badge, and when withdrawn.
    public string? Badge { get; init; }

    public required DateTime GivenAt { get; init; }
    public required DateTime SnapshotAt { get; init; }
}

public enum PraiseState
{
    Given,
    Withdrawn
}
