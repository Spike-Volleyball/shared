namespace Shared.Messaging.Contracts.Events.Coaching;

/// <summary>
/// The praise a coach gave with one piece of feedback, whole: published when feedback carrying praise
/// is shared and every time the praise changes, and once more, <see cref="PraiseState.Withdrawn"/>, when
/// the praise is taken off or the feedback unshared or deleted. Each copy replaces the last; a consumer
/// keeps the copy with the latest <see cref="SnapshotAt"/>.
/// </summary>
public record PraiseGivenEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid FeedbackId { get; init; }
    public required Guid CoachUserId { get; init; }
    public required Guid PlayerUserId { get; init; }
    public required PraiseState State { get; init; }

    /// The badge the coach chose — the BadgeType's name, e.g. "Star", "Hustle". Absent when withdrawn.
    public string? Badge { get; init; }

    public required DateTime GivenAt { get; init; }
    public required DateTime SnapshotAt { get; init; }
}

public enum PraiseState
{
    Given,
    Withdrawn
}
