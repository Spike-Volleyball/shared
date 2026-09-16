using FluentAssertions;
using Shared.Testing.Logging;

namespace Shared.Tests.Logging;

[TestFixture]
[Category("Unit")]
public class RawEmailLogStatementTests
{
    [Test]
    public void SharedSources_WriteNoRawEmailAddressToALog()
    {
        // Act
        var findings = EmailLogGuard.FindRawEmailLogStatements(EmailLogGuard.LocateSourceRoot("Shared.sln"));

        // Assert
        findings.Should().BeEmpty("an address may reach a log only through ILogPseudonymizer.PseudonymizeEmail");
    }
}
