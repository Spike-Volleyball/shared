using Shared.Enums;

namespace Shared.Messaging.Contracts.Events.Social.Posts;

// The post-side twin of CommentReplyEvent, which only covers event discussions: its
// TargetEventId and EventName are required and a club/team/group post has neither.
// Mirrors PostCommentedEvent's shape so both clients route it with no change — mobile
// reads postId + commentId, web reads contextType + contextId.
public class PostCommentRepliedEvent : INotificationEvent
{
    public Guid UserId { get; init; }
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Guid PostId { get; init; }

    // The reply, so the notification opens on what was written.
    public Guid CommentId { get; init; }

    // The comment being answered. Everyone who replies to it aggregates under one row,
    // which is what "{actors} replied to your comment" needs to be true.
    public Guid ParentCommentId { get; init; }

    public Guid ReplyAuthorId { get; init; }
    public required string ReplyAuthorName { get; init; }
    public ContextType ContextType { get; init; }
    public Guid ContextId { get; init; }
    public string? ReplyPreview { get; init; }
}
