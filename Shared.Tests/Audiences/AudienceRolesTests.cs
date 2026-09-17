using FluentAssertions;
using Shared.Audiences;
using Shared.Enums;

namespace Shared.Tests.Audiences;

[TestFixture]
[Category("Unit")]
public class AudienceRolesTests
{
    // The role names exactly as clubs-service declares them
    // (clubs-service/Clubs.Domain/Enums/Roles.cs). shared cannot reference Clubs.Domain, so this
    // half of the completeness invariant is literals; the reflecting half — the one that goes red
    // when someone adds a role — is Clubs.Tests.Unit/Audiences/AudienceRoleCoverageTests.cs.
    //
    // ClubRole.Guardian is deliberately absent. It is a roster-only marker that grants nothing and
    // Phase 2 deletes it; while it survives it belongs to neither audience.
    //
    // Coach joined the club vocabulary with the permission model (2026-08-31). Staff: a coach
    // coaches, and does not play.
    private static readonly string[] ClubRoleNames =
        ["Member", "WelfareOfficer", "Treasurer", "HeadCoach", "Coach", "Admin", "Owner"];

    private static readonly string[] GroupRoleNames =
        ["Member", "Helper", "Admin", "AssistantCoach", "Coach"];

    private static readonly string[] TeamRoleNames =
        ["Player", "Admin", "Captain", "Manager", "AssistantCoach", "Coach"];

    [Test]
    public void Matches_TeamPlayerRole_IsInPlayers()
    {
        // Arrange
        string[] player = ["Player"];
        string[] captain = ["Captain"];

        // Act & Assert
        AudienceRoles.Matches(Audience.Players, ContextType.Team, player).Should().BeTrue();
        AudienceRoles.Matches(Audience.Staff, ContextType.Team, player).Should().BeFalse();
        AudienceRoles.Matches(Audience.Players, ContextType.Team, captain).Should().BeTrue();
        AudienceRoles.Matches(Audience.Staff, ContextType.Team, captain).Should().BeFalse();
    }

    [Test]
    public void Matches_TeamCoachRole_IsInStaff()
    {
        // Arrange
        string[] staffRoles = ["Coach", "AssistantCoach", "Manager", "Admin"];

        // Act & Assert
        foreach (var role in staffRoles)
        {
            AudienceRoles.Matches(Audience.Staff, ContextType.Team, [role])
                .Should().BeTrue($"TeamRole.{role} is staff");
            AudienceRoles.Matches(Audience.Players, ContextType.Team, [role])
                .Should().BeFalse($"TeamRole.{role} is not a playing role");
        }
    }

    [Test]
    public void Matches_StaffStillHoldingTheDefaultRole_IsNotAPlayer()
    {
        // Arrange — assigning a staff role keeps the default one, so this is what every coach's row reads
        string[] coach = ["Player", "Coach"];

        // Act & Assert
        AudienceRoles.Matches(Audience.Players, ContextType.Team, coach).Should().BeFalse();
        AudienceRoles.Matches(Audience.Staff, ContextType.Team, coach).Should().BeTrue();
    }

    [Test]
    public void Matches_StaffWithARosterPosition_IsAPlayerAsWell()
    {
        // Arrange
        string[] coach = ["Player", "Coach"];
        string[] coachOnly = ["Coach"];

        // Act & Assert
        AudienceRoles.Matches(Audience.Players, ContextType.Team, coach, holdsRosterPosition: true).Should().BeTrue();
        AudienceRoles.Matches(Audience.Staff, ContextType.Team, coach, holdsRosterPosition: true).Should().BeTrue();
        AudienceRoles.Matches(Audience.Players, ContextType.Team, coachOnly, holdsRosterPosition: true)
            .Should().BeTrue("a coach placed on the roster plays, whatever else they hold");
    }

    [Test]
    public void Matches_RosterPosition_ChangesNothingForSomeoneWithoutAStaffRole()
    {
        // Act & Assert
        AudienceRoles.Matches(Audience.Players, ContextType.Team, ["Captain"], holdsRosterPosition: true).Should().BeTrue();
        AudienceRoles.Matches(Audience.Staff, ContextType.Team, ["Player"], holdsRosterPosition: true).Should().BeFalse();
        AudienceRoles.Matches(Audience.Players, ContextType.Team, ["Physio"], holdsRosterPosition: true)
            .Should().BeFalse("a position does not open the allow-list to a role it does not name");
    }

    [Test]
    public void Matches_ClubOrGroupStaffHoldingTheDefaultRole_IsNotAPlayer()
    {
        // Act & Assert
        AudienceRoles.Matches(Audience.Players, ContextType.Club, ["Member", "Treasurer"]).Should().BeFalse();
        AudienceRoles.Matches(Audience.Staff, ContextType.Club, ["Member", "Treasurer"]).Should().BeTrue();
        AudienceRoles.Matches(Audience.Players, ContextType.Group, ["Member", "Coach"]).Should().BeFalse();
        AudienceRoles.Matches(Audience.Players, ContextType.Group, ["Member"]).Should().BeTrue();
    }

    [Test]
    public void Matches_EmptyRoles_CountsAsTheContextDefault()
    {
        // Arrange
        string[] noRoles = [];

        // Act & Assert
        foreach (var contextType in new[] { ContextType.Club, ContextType.Group, ContextType.Team })
        {
            AudienceRoles.Matches(Audience.Players, contextType, noRoles)
                .Should().BeTrue($"a row with no roles in a {contextType} counts as its default role");
            AudienceRoles.Matches(Audience.Staff, contextType, noRoles)
                .Should().BeFalse($"a {contextType}'s default role is not a staff role");
        }
    }

    [Test]
    public void Matches_Members_IsTrueForEveryDirectRow()
    {
        // Act & Assert
        AudienceRoles.Matches(Audience.Members, ContextType.Team, ["Coach"]).Should().BeTrue();
        AudienceRoles.Matches(Audience.Members, ContextType.Team, []).Should().BeTrue();
        AudienceRoles.Matches(Audience.Members, ContextType.Club, ["Physio"]).Should().BeTrue();
    }

    [Test]
    public void Matches_UnknownRoleName_MatchesNeither()
    {
        // Arrange
        string[] unknown = ["Physio"];

        // Act & Assert
        AudienceRoles.Matches(Audience.Players, ContextType.Team, unknown).Should().BeFalse();
        AudienceRoles.Matches(Audience.Staff, ContextType.Team, unknown).Should().BeFalse();
    }

    [Test]
    public void Matches_RoleNameCasing_IsIgnored()
    {
        // Act & Assert
        AudienceRoles.Matches(Audience.Staff, ContextType.Team, ["coach"]).Should().BeTrue();
        AudienceRoles.Matches(Audience.Players, ContextType.Team, ["CAPTAIN"]).Should().BeTrue();
        AudienceRoles.Matches(Audience.Staff, ContextType.Club, ["welfareofficer"]).Should().BeTrue();
    }

    [Test]
    public void Matches_GuardiansOrEveryone_Throws()
    {
        // Arrange
        var guardians = () => AudienceRoles.Matches(Audience.Guardians, ContextType.Team, ["Player"]);
        var everyone = () => AudienceRoles.Matches(Audience.Everyone, ContextType.Team, ["Player"]);

        // Act & Assert
        guardians.Should().Throw<ArgumentOutOfRangeException>();
        everyone.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void PlayersAndStaff_TogetherCoverEveryRole_ForEveryContextType()
    {
        // Arrange
        (ContextType ContextType, string[] RoleNames)[] contexts =
        [
            (ContextType.Club, ClubRoleNames),
            (ContextType.Group, GroupRoleNames),
            (ContextType.Team, TeamRoleNames),
        ];

        foreach (var (contextType, roleNames) in contexts)
        {
            // Act
            var players = AudienceRoles.Players(contextType);
            var staff = AudienceRoles.Staff(contextType);

            // Assert
            foreach (var roleName in roleNames)
            {
                var tables = new[]
                    {
                        players.Contains(roleName) ? nameof(Audience.Players) : null,
                        staff.Contains(roleName) ? nameof(Audience.Staff) : null,
                    }
                    .Where(table => table is not null);

                tables.Should().ContainSingle(
                    $"{contextType}Role.{roleName} must be in exactly one audience table — " +
                    "add it to Shared/Audiences/AudienceRoles.cs");
            }

            players.Concat(staff).Should().BeEquivalentTo(
                roleNames,
                $"the {contextType} tables must name every {contextType} role and nothing else");

            players.Should().Contain(
                AudienceRoles.DefaultRole(contextType),
                $"a {contextType} row with no roles counts as its default role, which is a playing role");
        }
    }
}
