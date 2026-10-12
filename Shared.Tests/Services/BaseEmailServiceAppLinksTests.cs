using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Options;
using Shared.Services;
using Shared.Services.Logging;

namespace Shared.Tests.Services;

/// <summary>
/// Every mail of every service passes through <see cref="BaseEmailService"/>, so it is the one place
/// that can give each of them the band with the two store links. A template only names where the band
/// goes; no sender builds it and none can leave it out.
/// </summary>
[TestFixture]
[Category("Unit")]
public class BaseEmailServiceAppLinksTests
{
    private const string AppStoreAddress = "https://apps.apple.com/app/id6792373783";
    private const string GooglePlayAddress = "https://play.google.com/store/apps/details?id=app.volleyspike.mobile";
    private const string Words = "Get the Spike app";
    private const string Placeholder = "{{appLinks}}";
    private const string TemplateWithABand = "<div class=\"content\">{{greeting}}</div>{{appLinks}}<div class=\"footer\">Spike</div>";

    private readonly string _templateName = $"app-links-test-{Guid.NewGuid():N}";
    private RenderingEmailService _sut = null!;

    private string TemplateDirectory => Path.Combine(AppContext.BaseDirectory, "EmailTemplates", _templateName);

    [SetUp]
    public void SetUp()
    {
        Directory.CreateDirectory(TemplateDirectory);
        _sut = new RenderingEmailService();
    }

    [TearDown]
    public void TearDown() => Directory.Delete(TemplateDirectory, recursive: true);

    [Test]
    public async Task SendEmailAsync_WhenTheTemplateNamesTheBand_FillsItOnceBetweenTheContentAndTheFooter()
    {
        // Arrange
        WriteTemplate(TemplateWithABand);

        // Act
        await _sut.SendEmailAsync("reader@example.com", _templateName, new Dictionary<string, string> { ["greeting"] = "hi" });

        // Assert
        _sut.Body.Should().NotContain("{{", "every placeholder is filled");
        Occurrences(_sut.Body, Words).Should().Be(1);
        Occurrences(_sut.Body, AppStoreAddress).Should().Be(1);
        Occurrences(_sut.Body, GooglePlayAddress).Should().Be(1);
        var positions = new[] { "hi", Words, AppStoreAddress, GooglePlayAddress, "<div class=\"footer\">" }
            .Select(part => _sut.Body.IndexOf(part, StringComparison.Ordinal))
            .ToArray();
        positions.Should().OnlyContain(position => position >= 0, "each of them is in the mail");
        positions.Should().BeInAscendingOrder("the content, the words, App Store, Google Play, the footer");
    }

    [Test]
    public async Task SendEmailAsync_WithNoData_StillFillsTheBand()
    {
        // Arrange
        WriteTemplate("<p>Hello</p>{{appLinks}}");

        // Act
        await _sut.SendEmailAsync("reader@example.com", _templateName, new Dictionary<string, string>());

        // Assert
        _sut.Body.Should().NotContain("{{");
        Occurrences(_sut.Body, AppStoreAddress).Should().Be(1);
        Occurrences(_sut.Body, GooglePlayAddress).Should().Be(1);
    }

    [Test]
    public async Task SendEmailAsync_WhenTheCallersDataNamesAppLinks_StillCarriesTheServicesBand()
    {
        // Arrange
        const string callersBand = "a caller's own band";
        WriteTemplate(TemplateWithABand);
        var data = new Dictionary<string, string> { ["greeting"] = "hi", ["appLinks"] = callersBand };

        // Act
        await _sut.SendEmailAsync("reader@example.com", _templateName, data);

        // Assert
        _sut.Body.Should().NotContain(callersBand, "the band is the mail service's, not something a caller can replace");
        Occurrences(_sut.Body, AppStoreAddress).Should().Be(1);
        Occurrences(_sut.Body, GooglePlayAddress).Should().Be(1);
    }

    [Test]
    public async Task SendEmailAsync_WhenACallersTextSpellsThePlaceholder_KeepsItAsTypedAndCarriesTheBandOnce()
    {
        // Arrange
        WriteTemplate("<h2>{{greeting}}</h2>{{appLinks}}");
        var data = new Dictionary<string, string> { ["greeting"] = Placeholder };

        // Act
        await _sut.SendEmailAsync("reader@example.com", _templateName, data);

        // Assert
        _sut.Body.Should().StartWith($"<h2>{Placeholder}</h2>", "a squad named {{appLinks}} reads as its name");
        Occurrences(_sut.Body, Words).Should().Be(1);
        Occurrences(_sut.Body, AppStoreAddress).Should().Be(1);
    }

    [Test]
    public async Task SendEmailAsync_WhenTheTemplateHasNoPlaceholder_SendsItAsItWas()
    {
        // Arrange
        WriteTemplate("<p>{{greeting}}</p>");

        // Act
        await _sut.SendEmailAsync("reader@example.com", _templateName, new Dictionary<string, string> { ["greeting"] = "hi" });

        // Assert
        _sut.Body.Should().Be("<p>hi</p>");
    }

    [Test]
    public async Task SendEmailAsync_WhenTheSubjectSpellsThePlaceholder_LeavesTheSubjectAsItIs()
    {
        // Arrange
        WriteTemplate(TemplateWithABand, subject: "Hello {{appLinks}}");

        // Act
        await _sut.SendEmailAsync("reader@example.com", _templateName, new Dictionary<string, string> { ["greeting"] = "hi" });

        // Assert
        _sut.Subject.Should().Be("Hello {{appLinks}}", "a subject is one line of plain text, never a band of markup");
        Occurrences(_sut.Body, AppStoreAddress).Should().Be(1);
    }

    [Test]
    public async Task SendEmailAsync_LeavesTheCallersDataAlone()
    {
        // Arrange
        WriteTemplate(TemplateWithABand);
        var data = new Dictionary<string, string> { ["greeting"] = "hi" };

        // Act
        await _sut.SendEmailAsync("reader@example.com", _templateName, data);

        // Assert
        data.Should().BeEquivalentTo(new Dictionary<string, string> { ["greeting"] = "hi" });
    }

    private void WriteTemplate(string body, string? subject = null)
    {
        File.WriteAllText(Path.Combine(TemplateDirectory, "template.html"), body);
        if (subject is not null)
        {
            File.WriteAllText(Path.Combine(TemplateDirectory, "subject.txt"), subject);
        }
    }

    private static int Occurrences(string text, string value) => Regex.Count(text, Regex.Escape(value));

    private sealed class RenderingEmailService()
        : BaseEmailService(
            Substitute.For<ILogger>(),
            Microsoft.Extensions.Options.Options.Create(new EmailSettings { FromEmail = "noreply@example.com" }),
            Substitute.For<ILogPseudonymizer>())
    {
        public string Subject { get; private set; } = string.Empty;
        public string Body { get; private set; } = string.Empty;

        protected override Task SendEmailCoreAsync(string toEmail, string fromEmail, string subject, string htmlBody)
        {
            Subject = subject;
            Body = htmlBody;
            return Task.CompletedTask;
        }
    }
}
