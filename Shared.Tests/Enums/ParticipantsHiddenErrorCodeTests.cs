using FluentAssertions;
using Shared.Enums;

namespace Shared.Tests.Enums;

/// <summary>
/// The refusal events-service answers a stranger asking who is on a match. Clients branch on its wire code and
/// the events side of the web and the phone agreed its name and number, so neither may drift.
/// </summary>
[TestFixture]
[Category("Unit")]
public class ParticipantsHiddenErrorCodeTests
{
    [Test]
    public void ParticipantsHidden_IsNumberedInTheEventsRange_AfterTheLastCodeThere()
    {
        // Assert
        ((int)ErrorCodeEnum.ParticipantsHidden).Should().Be(319);
        ((int)ErrorCodeEnum.ParticipantsHidden).Should().BeGreaterThan((int)ErrorCodeEnum.MatchAlreadyScored);
    }

    [Test]
    public void ToStringCode_OfParticipantsHidden_IsTheWireCode()
    {
        // Act
        var code = ErrorCodeEnum.ParticipantsHidden.ToStringCode();

        // Assert
        code.Should().Be("PARTICIPANTS_HIDDEN");
    }

    [Test]
    public void ToTitle_OfParticipantsHidden_SaysWhatWasRefused()
    {
        // Act
        var title = ErrorCodeEnum.ParticipantsHidden.ToTitle();

        // Assert
        title.Should().Be("Participants Hidden");
    }

    [Test]
    public void ErrorCodeEnum_GivesNoTwoCodesTheSameNumber()
    {
        // Arrange
        var numbers = Enum.GetValues<ErrorCodeEnum>().Select(code => (int)code).ToList();

        // Assert
        numbers.Should().OnlyHaveUniqueItems();
    }
}
