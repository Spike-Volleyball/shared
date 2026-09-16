using FluentAssertions;
using Shared.Testing.Logging;

namespace Shared.Tests.Logging;

[TestFixture]
[Category("Unit")]
public class RawPersonalDataLogStatementTests
{
    [Test]
    public void SharedSources_WriteNoRawEmailAddressOrPhoneNumberToALog()
    {
        // Act
        var findings = PersonalDataLogGuard.FindRawPersonalDataLogStatements(
            PersonalDataLogGuard.LocateSourceRoot("Shared.sln"));

        // Assert
        findings.Should().BeEmpty("personal identifiers may reach a log only through ILogPseudonymizer");
    }
}
