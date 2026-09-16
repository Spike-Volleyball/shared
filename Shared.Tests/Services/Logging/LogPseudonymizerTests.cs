using FluentAssertions;
using Shared.Options;
using Shared.Services.Logging;

namespace Shared.Tests.Services.Logging;

[TestFixture]
[Category("Unit")]
public class LogPseudonymizerTests
{
    private const string Key = "spike-test-log-pseudonymization-key-0001";

    private static LogPseudonymizer Create(string? key) =>
        new(Microsoft.Extensions.Options.Options.Create(new LogPseudonymizationSettings { HmacKey = key }));

    /// <summary>
    /// The vector ops reproduces to find one address in the logs:
    /// <c>printf '%s' john.doe@example.com | openssl dgst -sha256 -hmac "$KEY" -r | cut -c1-16</c>.
    /// </summary>
    [Test]
    public void PseudonymizeEmail_WithAKey_ReturnsTheMaskAndTheKeyedHash()
    {
        // Arrange
        var sut = Create(Key);

        // Act
        var result = sut.PseudonymizeEmail("john.doe@example.com");

        // Assert
        result.Should().Be("j***@e***.com hmac:52447c619733b888");
    }

    [Test]
    public void PseudonymizeEmail_WithTheSameAddressWrittenDifferently_ReturnsTheSameValue()
    {
        // Arrange
        var sut = Create(Key);

        // Act
        var typed = sut.PseudonymizeEmail("  John.Doe@Example.COM ");

        // Assert
        typed.Should().Be(sut.PseudonymizeEmail("john.doe@example.com"));
    }

    [Test]
    public void PseudonymizeEmail_WithAddressesSharingAMask_KeepsThemApart()
    {
        // Arrange
        var sut = Create(Key);

        // Act
        var first = sut.PseudonymizeEmail("john.doe@example.com");
        var second = sut.PseudonymizeEmail("jane.roe@example.com");

        // Assert
        first.Should().NotBe(second);
    }

    /// <summary>
    /// Keyed, so a list of candidate addresses hashed by someone who only holds the logs matches nothing.
    /// </summary>
    [Test]
    public void PseudonymizeEmail_UnderADifferentKey_ReturnsADifferentHash()
    {
        // Act
        var first = Create(Key).PseudonymizeEmail("john.doe@example.com");
        var second = Create("spike-test-log-pseudonymization-key-0002").PseudonymizeEmail("john.doe@example.com");

        // Assert
        first.Should().NotBe(second);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("short-key")]
    public void PseudonymizeEmail_WithoutAUsableKey_ReturnsOnlyTheMask(string? key)
    {
        // Arrange
        var sut = Create(key);

        // Act
        var result = sut.PseudonymizeEmail("john.doe@example.com");

        // Assert
        result.Should().Be("j***@e***.com");
    }

    [TestCase("john.doe@example.com", "j***@e***.com")]
    [TestCase("a@b.co.uk", "a***@b***.uk")]
    [TestCase("x@localhost", "x***@l***")]
    [TestCase("@example.com", "***@e***.com")]
    [TestCase("john@", "j***@***")]
    [TestCase("quoted@local@example.org", "q***@e***.org")]
    [TestCase("not-an-address", "***")]
    [TestCase("ünïcode@exämple.de", "ü***@e***.de")]
    [TestCase("😀smile@example.com", "😀***@e***.com")]
    public void PseudonymizeEmail_WithoutAKey_MasksEverythingButTheFirstLettersAndTheTopLevelDomain(
        string email, string expected)
    {
        // Act
        var result = Create(null).PseudonymizeEmail(email);

        // Assert
        result.Should().Be(expected);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void PseudonymizeEmail_WithNoAddress_SaysSo(string? email)
    {
        // Act
        var result = Create(Key).PseudonymizeEmail(email);

        // Assert
        result.Should().Be("(empty)");
    }

    [TestCase("john.doe@example.com")]
    [TestCase("parent.of.a.minor@school-district.org")]
    public void PseudonymizeEmail_NeverRepeatsTheAddressOrEitherOfItsParts(string email)
    {
        // Arrange
        var at = email.IndexOf('@');

        // Act
        var result = Create(Key).PseudonymizeEmail(email);

        // Assert
        result.Should().NotContain(email[..at]);
        result.Should().NotContain(email[(at + 1)..]);
    }

    /// <summary>
    /// The number is hashed as '+' (when given) and its digits only:
    /// <c>printf '%s' +447700900123 | openssl dgst -sha256 -hmac "$KEY" -r | cut -c1-16</c>.
    /// </summary>
    [Test]
    public void PseudonymizePhoneNumber_WithAKey_ReturnsTheMaskAndTheKeyedHash()
    {
        // Arrange
        var sut = Create(Key);

        // Act
        var result = sut.PseudonymizePhoneNumber("+447700900123");

        // Assert
        result.Should().Be("+***23 hmac:8f05020e3368373f");
    }

    [TestCase(" +44 7700 900123 ")]
    [TestCase("+44 (7700) 900-123")]
    [TestCase("+44.7700.900.123")]
    public void PseudonymizePhoneNumber_WithTheSameNumberWrittenDifferently_ReturnsTheSameValue(string typed)
    {
        // Arrange
        var sut = Create(Key);

        // Act
        var result = sut.PseudonymizePhoneNumber(typed);

        // Assert
        result.Should().Be(sut.PseudonymizePhoneNumber("+447700900123"));
    }

    [Test]
    public void PseudonymizePhoneNumber_WithNumbersSharingAMask_KeepsThemApart()
    {
        // Arrange
        var sut = Create(Key);

        // Act
        var first = sut.PseudonymizePhoneNumber("+447700900123");
        var second = sut.PseudonymizePhoneNumber("+447700900223");

        // Assert
        first.Should().NotBe(second);
    }

    [TestCase(null)]
    [TestCase("short-key")]
    public void PseudonymizePhoneNumber_WithoutAUsableKey_ReturnsOnlyTheMask(string? key)
    {
        // Act
        var result = Create(key).PseudonymizePhoneNumber("+447700900123");

        // Assert
        result.Should().Be("+***23");
    }

    [TestCase("+447700900123", "+***23")]
    [TestCase("07700 900123", "***23")]
    [TestCase("+1 (555) 010-9999", "+***99")]
    [TestCase("12345", "***")]
    [TestCase("ext. only", "***")]
    public void PseudonymizePhoneNumber_WithoutAKey_MasksEverythingButTheLastTwoDigits(string phoneNumber, string expected)
    {
        // Act
        var result = Create(null).PseudonymizePhoneNumber(phoneNumber);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>Nothing to correlate: a hash of no digits would be the same for every such value.</summary>
    [Test]
    public void PseudonymizePhoneNumber_WithNoDigitsAtAll_ReturnsTheMaskEvenWithAKey()
    {
        // Act
        var result = Create(Key).PseudonymizePhoneNumber("unknown");

        // Assert
        result.Should().Be("***");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void PseudonymizePhoneNumber_WithNoNumber_SaysSo(string? phoneNumber)
    {
        // Act
        var result = Create(Key).PseudonymizePhoneNumber(phoneNumber);

        // Assert
        result.Should().Be("(empty)");
    }

    [Test]
    public void PseudonymizePhoneNumber_NeverRepeatsTheNumberOrItsSubscriberPart()
    {
        // Act
        var result = Create(Key).PseudonymizePhoneNumber("+44 7700 900123");

        // Assert
        result.Should().NotContain("447700900123");
        result.Should().NotContain("7700");
        result.Should().NotContain("900123");
    }
}
