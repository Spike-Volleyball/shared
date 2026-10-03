namespace Shared.Models;

/// <summary>
/// Words Spike says on every club's behalf that more than one service has to agree on.
/// </summary>
public static class ReminderWording
{
    /// <summary>
    /// What a reminder to the people who haven't answered says when nobody wrote its words; the
    /// notification adds the event's date after it. The notification falls back to it, a club's
    /// settings offer it to start from, and the services treat a message equal to it as none.
    /// </summary>
    public const string RespondDefault = "You haven't answered yet";
}
