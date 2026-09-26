namespace Shared.Authorization;

/// <summary>
/// The atoms of authorization. Code asks for the permission it needs and never asks who you are,
/// which is what stops a seventh hardcoded role list from being written.
///
/// Lives in shared because these strings cross the wire: clubs-service computes them from roles
/// and every other service consumes them. The role-to-permission MAP stays in clubs — it is the
/// scope owner's business and nobody else's — but the vocabulary has to be shared or each
/// consumer ends up hardcoding the literals it happens to care about, which is the stringly-typed
/// version of the problem this replaces.
/// Renaming one is a breaking change; adding one is not, which is the point: new functionality
/// ships a new permission rather than editing a role, and everyone holding a role keeps what
/// they had.
///
/// The grants behind each are in <see cref="PermissionMap"/>, and the matrix they encode was
/// ruled on 2026-08-31.
/// </summary>
public static class Permission
{
    // ---- club scope: held once, applies across the club and everything in it ----
    public const string MembersView = "members.view";
    public const string MembersViewSensitive = "members.view_sensitive";
    // The club roster itself: admitting people, removing them, approving registrations, sending
    // invitations, editing forms, restricting guardians. Nineteen call sites hang off this.
    public const string MembersManage = "members.manage";

    // Putting someone who is ALREADY a club member into a team or group. A coach staffs their
    // own units without thereby running the club — the two were one permission until the
    // migration inventory showed members.manage also gates registrations and invitations.
    public const string MembersAssign = "members.assign";

    // The club's shape: creating a team or group, renaming one, standing one down. Distinct from
    // settings.manage (the club's own configuration) and from members.assign (who goes in one) —
    // a head coach reorganises the squads without touching either.
    public const string UnitsManage = "units.manage";
    public const string RolesAssign = "roles.assign";
    public const string CoachingAssign = "coaching.assign";
    public const string EventsViewAll = "events.view_all";
    public const string EventsCreate = "events.create";
    public const string EventsManage = "events.manage";
    public const string FeedbackGive = "feedback.give";
    public const string LibraryManage = "library.manage";
    public const string ContentPost = "content.post";
    public const string ContentModerate = "content.moderate";

    // Reaching every person in a context at once — @everyone, @guardians, the context-wide
    // mention groups. Distinct from moderation: addressing a room and removing someone from it
    // are different powers, and social and messages currently keep two different sets for it.
    public const string ContentBroadcast = "content.broadcast";
    public const string FinanceManage = "finance.manage";

    // Below finance.manage: taking an event's payments into the club's own account. The money
    // can land nowhere else, so this is running a paid session for the club, not running its
    // books - which is why a head coach holds it without finance.manage (SPI-6619).
    public const string FinanceCollect = "finance.collect";

    // Above finance.manage: linking where the club's money actually lands. payments keeps this
    // as an Owner-only tier already (PaymentAccountsController:74), and running the books is not
    // the same as choosing the bank account they settle into.
    public const string FinanceAccountsManage = "finance.accounts.manage";
    public const string SettingsManage = "settings.manage";
    public const string ClubDelete = "club.delete";

    // ownership.transfer is deliberately absent. Denys defines Admin as "Owner minus deleting the
    // club and transferring ownership", and the second half has no feature behind it — two
    // comments in ClubService reference a TransferOwnership flow that does not exist, with no
    // service method and no endpoint. A permission with no enforcement site is a constant that
    // rots; it goes in with the feature. club.delete is real (ClubsController.cs:96), so the
    // Admin boundary still has something to stand on.

    // ---- unit scope: held on one team or group, applies only to it ----

    // The floor. Being in a team or group is reason enough to see it, who is in it and what it
    // has on — no role required, which is the point: membership is not an office.
    public const string UnitMembersView = "unit.members.view";
    public const string UnitEventsView = "unit.events.view";

    public const string UnitMembersViewSensitive = "unit.members.view_sensitive";
    public const string UnitMembersManage = "unit.members.manage";

    // The unit's own details — its name, description, branding, whether it is still running.
    // The unit-scoped twin of units.manage, which is what lets a coach look after their own team
    // without being able to reorganise the club's.
    public const string UnitSettingsManage = "unit.settings.manage";
    public const string UnitRosterManage = "unit.roster.manage";
    public const string UnitEventsCreate = "unit.events.create";
    public const string UnitEventsManage = "unit.events.manage";
    public const string UnitFeedbackGive = "unit.feedback.give";
    public const string UnitContentModerate = "unit.content.moderate";
    public const string UnitContentBroadcast = "unit.content.broadcast";
    public const string UnitRolesAssign = "unit.roles.assign";

    /// <summary>
    /// Whether a string names a permission this vocabulary defines. The gate on direct grants:
    /// they are stored as text, so this is what stops a typo or a stale grant from a renamed
    /// permission being read back as authority.
    /// </summary>
    public static bool IsKnown(string permission) => All.Contains(permission);

    /// <summary>Every club-scoped permission. An Owner holds exactly this set.</summary>
    public static readonly IReadOnlySet<string> AllClubScoped = new HashSet<string>
    {
        MembersView, MembersViewSensitive, MembersManage, MembersAssign, UnitsManage, RolesAssign,
        CoachingAssign,
        EventsViewAll, EventsCreate, EventsManage, FeedbackGive, LibraryManage,
        ContentPost, ContentModerate, ContentBroadcast,
        FinanceManage, FinanceCollect, FinanceAccountsManage, SettingsManage,
        ClubDelete,
    };

    /// <summary>Every permission at either scope — the whole vocabulary.</summary>
    public static readonly IReadOnlySet<string> All = new HashSet<string>(
        AllClubScoped.Concat(
        [
            UnitMembersView, UnitEventsView, UnitMembersViewSensitive, UnitMembersManage,
            UnitSettingsManage, UnitRosterManage, UnitEventsCreate, UnitEventsManage,
            UnitFeedbackGive,
            UnitContentModerate, UnitContentBroadcast, UnitRolesAssign,
        ]));
}
