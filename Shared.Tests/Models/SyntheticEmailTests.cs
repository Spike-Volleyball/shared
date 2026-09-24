using FluentAssertions;
using Shared.Models;

namespace Shared.Tests.Models;

[TestFixture]
[Category("Unit")]
public class SyntheticEmailTests
{
    [Test]
    public void IsSynthetic_TheAddressesMadeUpForAccounts_AreSynthetic()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act & Assert
        SyntheticEmail.IsSynthetic(SyntheticEmail.ForManaged(userId)).Should().BeTrue();
        SyntheticEmail.IsSynthetic(SyntheticEmail.ForPlaceholder(userId)).Should().BeTrue();
    }

    [TestCase("anyone@spike.local")]
    [TestCase("anyone@other.spike.local")]
    [TestCase("Someone@PLACEHOLDER.Spike.Local ")]
    public void IsSynthetic_AnyAddressUnderSpikeLocal_IsSynthetic(string email)
    {
        // Act & Assert
        SyntheticEmail.IsSynthetic(email).Should().BeTrue();
    }

    [TestCase("anna@example.com")]
    [TestCase("spike.local@example.com")]
    [TestCase("anna@notspike.local")]
    [TestCase("not-an-address")]
    [TestCase("")]
    [TestCase(null)]
    public void IsSynthetic_AnyOtherAddress_IsNot(string? email)
    {
        // Act & Assert
        SyntheticEmail.IsSynthetic(email).Should().BeFalse();
    }

    [Test]
    public void ForPlaceholder_IsDistinctFromForManaged()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act & Assert
        SyntheticEmail.ForPlaceholder(userId).Should().NotBe(SyntheticEmail.ForManaged(userId))
            .And.StartWith("placeholder-")
            .And.EndWith("@placeholder.spike.local");
    }
}
