namespace Shared.Messaging.Contracts.Events.Tournaments;

/// <summary>
/// Who should sit in a squad's team chat changed: accounts came onto the squad's sheet or went off it,
/// or the squad linked its chat, which names everyone on the sheet at that moment. Published only for
/// a squad that has a chat. Whoever keeps the chat seats and unseats them, so the chat follows the
/// sheet without the captain's app having to be open.
/// </summary>
/// <remarks>
/// A change, not the whole sheet: a player who left the chat of their own accord and is still on the
/// sheet is named in neither list, and stays out.
/// </remarks>
public record TournamentSquadChatSeatsChangedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid ChatId { get; init; }
    public required Guid TournamentId { get; init; }
    public required Guid EntrantId { get; init; }

    /// <summary>Accounts that are on the sheet and should sit in the chat. One already there stays as they are.</summary>
    public IReadOnlyList<Guid> JoinedUserIds { get; init; } = [];

    /// <summary>Accounts taken off the sheet. Never the squad's captain, whose chat it is.</summary>
    public IReadOnlyList<Guid> RemovedUserIds { get; init; } = [];
}
