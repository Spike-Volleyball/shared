using Shared.Services.FileStorage;

namespace Shared.Tests.Services.FileStorage;

[TestFixture]
[Category("Unit")]
public class ContentDispositionValueTests
{
    [Test]
    public void Inline_WithAsciiFileName_QuotesFileName()
    {
        var result = ContentDispositionValue.Inline("report.pdf");

        Assert.That(result, Is.EqualTo("inline; filename=\"report.pdf\""));
    }

    [Test]
    public void Inline_WithNonAsciiFileName_AddsRfc5987EncodedVariant()
    {
        var result = ContentDispositionValue.Inline("звіт.pdf");

        Assert.That(result, Is.EqualTo(
            "inline; filename=\"____.pdf\"; filename*=UTF-8''%D0%B7%D0%B2%D1%96%D1%82.pdf"));
    }

    [Test]
    public void Inline_WithQuotesAndBackslashes_SanitizesFallback()
    {
        var result = ContentDispositionValue.Inline("my\"file\\v1.pdf");

        Assert.That(result, Is.EqualTo(
            "inline; filename=\"my_file_v1.pdf\"; filename*=UTF-8''my%22file%5Cv1.pdf"));
    }

    [Test]
    public void Inline_StripsControlCharacters()
    {
        var result = ContentDispositionValue.Inline("a\r\nb.pdf");

        Assert.That(result, Is.EqualTo("inline; filename=\"ab.pdf\""));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Inline_WithMissingFileName_OmitsFileName(string? fileName)
    {
        var result = ContentDispositionValue.Inline(fileName);

        Assert.That(result, Is.EqualTo("inline"));
    }

    [Test]
    public void Attachment_WithAsciiFileName_QuotesFileName()
    {
        var result = ContentDispositionValue.Attachment("logo.svg");

        Assert.That(result, Is.EqualTo("attachment; filename=\"logo.svg\""));
    }

    [Test]
    public void Attachment_WithNonAsciiFileName_AddsRfc5987EncodedVariant()
    {
        var result = ContentDispositionValue.Attachment("звіт.svg");

        Assert.That(result, Is.EqualTo(
            "attachment; filename=\"____.svg\"; filename*=UTF-8''%D0%B7%D0%B2%D1%96%D1%82.svg"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Attachment_WithMissingFileName_OmitsFileName(string? fileName)
    {
        var result = ContentDispositionValue.Attachment(fileName);

        Assert.That(result, Is.EqualTo("attachment"));
    }

    [Test]
    public void For_WithSvg_ReturnsAttachment()
    {
        var result = ContentDispositionValue.For(ContentTypes.Svg, "logo.svg");

        Assert.That(result, Is.EqualTo("attachment; filename=\"logo.svg\""));
    }

    [TestCase("image/png")]
    [TestCase("application/pdf")]
    public void For_WithOtherContentType_ReturnsInline(string contentType)
    {
        var result = ContentDispositionValue.For(contentType, "file.bin");

        Assert.That(result, Is.EqualTo("inline; filename=\"file.bin\""));
    }
}
