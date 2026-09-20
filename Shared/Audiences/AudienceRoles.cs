using Shared.Enums;

namespace Shared.Audiences;

/// <summary>
/// Which roles put a direct member in the Players or Staff audience, per context type.
/// Both sides are allow-lists: "everything that is not a Player" is the shape that silently
/// granted a guardian seat event-creation rights in August (spec §1.2), and a new role must
/// fail a test rather than land in an audience by default.
///
/// This is a FIFTH classification of roles and it supersedes none of the four that exist
/// (ClubService.RoleHierarchy, UserRelationshipService.RoleRank, ModeratorRoles, and the raw
/// enum-value ordering in the gRPC projections). Staff is not the moderator set and is not
/// the may-create-an-event set. Do not merge them.
///
/// Comparison is by role NAME, case-insensitively: the role enums live in clubs-service and
/// the wire carries their names, so a name is the only key every consumer holds.
/// </summary>
public static class AudienceRoles
{
    private const string ClubMemberRole = "Member";
    private const string GroupMemberRole = "Member";
    private const string TeamPlayerRole = "Player";

    private static readonly IReadOnlySet<string> ClubPlayerRoles = NameSet(ClubMemberRole);

    // Coach joined the club vocabulary with the permission model (2026-08-31). It is staff:
    // a coach coaches, and does not play.
    private static readonly IReadOnlySet<string> ClubStaffRoles =
        NameSet("Owner", "Admin", "HeadCoach", "Coach", "Treasurer", "WelfareOfficer");

    private static readonly IReadOnlySet<string> GroupPlayerRoles = NameSet(GroupMemberRole);

    private static readonly IReadOnlySet<string> GroupStaffRoles = NameSet("Admin", "Coach", "AssistantCoach", "Helper");

    private static readonly IReadOnlySet<string> TeamPlayerRoles = NameSet(TeamPlayerRole, "Captain");

    private static readonly IReadOnlySet<string> TeamStaffRoles = NameSet("Admin", "Manager", "Coach", "AssistantCoach");

    public static IReadOnlySet<string> Players(ContextType contextType) => contextType switch
    {
        ContextType.Club => ClubPlayerRoles,
        ContextType.Group => GroupPlayerRoles,
        ContextType.Team => TeamPlayerRoles,
        _ => throw UnsupportedContext(contextType),
    };

    public static IReadOnlySet<string> Staff(ContextType contextType) => contextType switch
    {
        ContextType.Club => ClubStaffRoles,
        ContextType.Group => GroupStaffRoles,
        ContextType.Team => TeamStaffRoles,
        _ => throw UnsupportedContext(contextType),
    };

    /// <summary>The role a direct row with no role rows is counted as.</summary>
    public static string DefaultRole(ContextType contextType) => contextType switch
    {
        ContextType.Club => ClubMemberRole,
        ContextType.Group => GroupMemberRole,
        ContextType.Team => TeamPlayerRole,
        _ => throw UnsupportedContext(contextType),
    };

    /// <summary>
    /// True when a member holding <paramref name="roles"/> belongs in <paramref name="audience"/>.
    /// An empty role set is treated as DefaultRole. Guardians and Everyone are not answerable
    /// from roles alone and throw — the caller must have resolved derivation first.
    ///
    /// A staff role takes a member out of Players unless they hold a roster position. Assigning a
    /// staff role keeps the default one, so a coach's row reads ['Player', 'Coach'], and "holds a
    /// playing role" alone would put every coach in Players. A coach who is on the roster does
    /// play, and stays in both. Only a team roster has positions; clubs and groups pass false.
    /// </summary>
    public static bool Matches(
        Audience audience,
        ContextType contextType,
        IReadOnlyCollection<string> roles,
        bool holdsRosterPosition = false)
    {
        var held = roles.Count == 0 ? [DefaultRole(contextType)] : roles;
        var holdsStaffRole = held.Any(Staff(contextType).Contains);

        return audience switch
        {
            Audience.Members => true,
            Audience.Staff => holdsStaffRole,
            Audience.Players => holdsStaffRole ? holdsRosterPosition : held.Any(Players(contextType).Contains),
            _ => throw new ArgumentOutOfRangeException(
                nameof(audience),
                audience,
                "Guardians and Everyone are derived from membership, not from roles. Resolve the derivation first."),
        };
    }

    private static IReadOnlySet<string> NameSet(params string[] roleNames) =>
        new HashSet<string>(roleNames, StringComparer.OrdinalIgnoreCase);

    private static ArgumentOutOfRangeException UnsupportedContext(ContextType contextType) =>
        new(nameof(contextType), contextType, "Audiences are defined for the Club, Group and Team contexts only.");
}
