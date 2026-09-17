using FluentAssertions;
using Microsoft.Extensions.Logging;
using Shared.Options;
using Shared.Services;
using Shared.Services.Logging;
using Shared.Tests.Services.Analytics;

namespace Shared.Tests.Services;

/// <summary>
/// Every service that sends email logs each send through this class, so a recipient written here
/// lands in the logs of all of them.
/// </summary>
[TestFixture]
[Category("Unit")]
public class BaseEmailServiceTests
{
    private const string Recipient = "parent.of.a.minor@example.com";

    private readonly string _templateName = $"base-email-service-test-{Guid.NewGuid():N}";
    private readonly LogPseudonymizer _pseudonymizer = new(Microsoft.Extensions.Options.Options.Create(
        new LogPseudonymizationSettings { HmacKey = "spike-test-log-pseudonymization-key-0001" }));

    private RecordingLogger<StubEmailService> _logger = null!;

    private string TemplateDirectory => Path.Combine(AppContext.BaseDirectory, "EmailTemplates", _templateName);

    [SetUp]
    public void SetUp()
    {
        Directory.CreateDirectory(TemplateDirectory);
        File.WriteAllText(Path.Combine(TemplateDirectory, "template.html"), "<p>{{greeting}}</p>");
        _logger = new RecordingLogger<StubEmailService>();
    }

    [TearDown]
    public void TearDown() => Directory.Delete(TemplateDirectory, recursive: true);

    [Test]
    public async Task SendEmailAsync_WhenSent_LogsTheRecipientPseudonymized()
    {
        // Arrange
        var sut = new StubEmailService(_logger, _pseudonymizer, failure: null);

        // Act
        await sut.SendEmailAsync(Recipient, _templateName, new Dictionary<string, string> { ["greeting"] = "hi" });

        // Assert
        var entry = _logger.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Information);
        entry.Message.Should().Contain(_pseudonymizer.PseudonymizeEmail(Recipient));
        entry.Message.Should().NotContain(Recipient);
    }

    [Test]
    public async Task SendEmailAsync_WhenSendingFails_LogsTheRecipientPseudonymizedAndRethrows()
    {
        // Arrange
        var sut = new StubEmailService(_logger, _pseudonymizer, new InvalidOperationException("smtp down"));

        // Act
        var act = () => sut.SendEmailAsync(Recipient, _templateName, new Dictionary<string, string>());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        var entry = _logger.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Error);
        entry.Message.Should().Contain(_pseudonymizer.PseudonymizeEmail(Recipient));
        entry.Message.Should().NotContain(Recipient);
    }

    internal sealed class StubEmailService(
        ILogger<StubEmailService> logger,
        ILogPseudonymizer pseudonymizer,
        Exception? failure)
        : BaseEmailService(
            logger,
            Microsoft.Extensions.Options.Options.Create(new EmailSettings { FromEmail = "noreply@example.com" }),
            pseudonymizer)
    {
        protected override Task SendEmailCoreAsync(string toEmail, string fromEmail, string subject, string htmlBody) =>
            failure is null ? Task.CompletedTask : Task.FromException(failure);
    }
}
