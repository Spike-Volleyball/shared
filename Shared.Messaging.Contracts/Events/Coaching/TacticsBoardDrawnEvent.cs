namespace Shared.Messaging.Contracts.Events.Coaching;

/// <summary>
/// A tactics board somebody made, once it has a drawing on it, whole: published the first time its
/// maker saves it with one, and again whenever a save changes the tools its drawing uses. Never for
/// the starter boards a new shelf is seeded with. Each copy replaces the last; a consumer keeps the
/// copy with the latest <see cref="SnapshotAt"/>.
/// </summary>
/// <remarks>It notifies nobody, so history can be republished.</remarks>
public record TacticsBoardDrawnEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid BoardId { get; init; }
    public required Guid MakerUserId { get; init; }

    /// When it was first saved with a drawing on it: the same in every copy.
    public required DateTime DrawnAt { get; init; }

    /// What the drawing uses, as the editor names its tools: the kinds of mark on it ("arrow", "area",
    /// "spotlight", "ink") and the analysis switched on ("heatmap", "projection"). Empty from a client
    /// too old to say.
    public required IReadOnlyList<string> Tools { get; init; }

    public required DateTime SnapshotAt { get; init; }
}
