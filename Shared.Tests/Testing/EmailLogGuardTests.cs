using FluentAssertions;
using Shared.Testing.Logging;

namespace Shared.Tests.Testing;

[TestFixture]
[Category("Unit")]
public class EmailLogGuardTests
{
    private static IReadOnlyList<string> Inspect(string body) =>
        EmailLogGuard.Inspect($$"""
            class Subject
            {
                void Run()
                {
            {{body}}
                }
            }
            """, "Subject.cs").ToList();

    [TestCase("""_logger.LogWarning("Login attempt failed for email: {Email}", request.Email);""")]
    [TestCase("""_logger.LogWarning("Impersonation by {CallerEmail}", caller.Id);""")]
    [TestCase("""_logger.LogInformation("Created stub {Subject}", normalizedEmail);""")]
    [TestCase("""_logger.LogInformation("Invitation sent to {Guardian}", evt.GuardianEmail);""")]
    [TestCase("""_logger.Log(LogLevel.Warning, "Login failed for {Email}", request.Email);""")]
    [TestCase("""_logger?.LogError(ex, "Failed to send to {RecipientEmail}", to);""")]
    [TestCase("""using var scope = _logger.BeginScope("Sending to {Email}", email);""")]
    [TestCase("""_logger.LogWarning($"Login failed for {request.Email}");""")]
    [TestCase("""var log = LoggerMessage.Define<string>(LogLevel.Information, new EventId(1), "Sent to {Email}");""")]
    [TestCase("""logger.LogInformation("Sent to {@Recipient}", new { user.Email });""")]
    public void Inspect_WithARawAddress_FlagsTheStatement(string statement)
    {
        // Act
        var findings = Inspect(statement);

        // Assert
        findings.Should().ContainSingle();
    }

    [TestCase("""_logger.LogWarning("Login failed for {EmailRef}", _pseudonymizer.PseudonymizeEmail(request.Email));""")]
    [TestCase("""_logger.LogWarning("Login failed for user {UserId}", user.Id);""")]
    [TestCase("""_logger.LogInformation("User {UserId} verified: {Verified}", user.Id, user.IsEmailVerified);""")]
    [TestCase("""_logger.LogInformation("Sent {Template} email", EmailTemplateName.ResetPassword);""")]
    [TestCase("""_logger.LogInformation("Literal {{Email}} braces are not a placeholder");""")]
    [TestCase("""await _emailService.SendEmailAsync(email, template, parameters);""")]
    public void Inspect_WithoutARawAddress_FindsNothing(string statement)
    {
        // Act
        var findings = Inspect(statement);

        // Assert
        findings.Should().BeEmpty();
    }

    [Test]
    public void Inspect_WithAnAddressOnlyInAComment_FindsNothing()
    {
        // Act
        var findings = Inspect("""
                    _logger.LogInformation(
                        // the recipient's email is left out on purpose
                        "Sent invitation {InvitationId}", invitationId);
            """);

        // Assert
        findings.Should().BeEmpty();
    }

    [Test]
    public void Inspect_WithALoggerMessageAttribute_ChecksItsTemplate()
    {
        // Act
        var findings = EmailLogGuard.Inspect("""
            static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Sent to {Email}")]
                public static partial void Sent(ILogger logger, string email);
            }
            """, "Log.cs").ToList();

        // Assert
        findings.Should().ContainSingle().Which.Should().StartWith("Log.cs:3:");
    }

    [Test]
    public void Inspect_WithAStatementOverSeveralLines_ReportsTheLineItStartsOn()
    {
        // Act
        var findings = Inspect("""
                    _logger.LogInformation(
                        "Guardian invitation email sent for invitation {InvitationId} to {Email}",
                        evt.InvitationId, evt.GuardianEmail);
            """);

        // Assert
        findings.Should().ContainSingle().Which.Should().StartWith("Subject.cs:5:");
    }

    [Test]
    public void FindRawEmailLogStatements_SkipsBuildOutputAndTheDirectoriesItIsTold()
    {
        // Arrange
        var root = Directory.CreateTempSubdirectory("email-log-guard-").FullName;
        const string leak = """class C { void M() { _logger.LogInformation("To {Email}", email); } }""";
        try
        {
            foreach (var directory in new[] { "Service", "obj", "bin", "shared" })
            {
                Directory.CreateDirectory(Path.Combine(root, directory));
                File.WriteAllText(Path.Combine(root, directory, "Leak.cs"), leak);
            }

            // Act
            var findings = EmailLogGuard.FindRawEmailLogStatements(root, "shared");

            // Assert
            findings.Should().ContainSingle()
                .Which.Should().StartWith(Path.Combine("Service", "Leak.cs") + ":1:");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
