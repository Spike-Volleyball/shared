using Shared.Messaging.Contracts.Events.Tournaments;

namespace Shared.Messaging.Contracts.Events.Events;

/// <summary>
/// A notification about one event. The web inbox and the phone find the screen an events.*
/// notification opens by the event's id, which the generic notice does not carry: it knows only a link.
/// </summary>
public interface IEventNotice : IUserNotification
{
    /// The event the notice is about, which <see cref="IEvent.EventId"/> cannot be: that one identifies this message.
    Guid TargetEventId { get; }

    string EventName { get; }
}
