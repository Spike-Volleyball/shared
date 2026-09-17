using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Testing.Logging;

namespace Shared.Tests.Testing;

[TestFixture]
[Category("Unit")]
public class LoggerSubstituteExtensionsTests
{
    [Test]
    public void LoggedMessages_RendersEachMessageWithItsValues()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggerSubstituteExtensionsTests>>();
        var userId = Guid.NewGuid();

        // Act
        logger.LogWarning("Login attempt by user {UserId} with an unverified email", userId);
        logger.LogInformation("Sent {Template} email", "ResetPassword");

        // Assert
        logger.LoggedMessages().Should().Equal(
            $"Login attempt by user {userId} with an unverified email",
            "Sent ResetPassword email");
    }

    [Test]
    public void LoggedMessages_IgnoresCallsThatWriteNothing()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggerSubstituteExtensionsTests>>();

        // Act
        logger.IsEnabled(LogLevel.Debug);
        using (logger.BeginScope("Scope {Id}", 1))
        {
        }

        // Assert
        logger.LoggedMessages().Should().BeEmpty();
    }
}
