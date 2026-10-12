namespace Shared.Services;

/// <summary>
/// The band a mail carries between its content and its footer: the words "Get the Spike app" and
/// a button for each store. It is said here once. A template only names where it goes
/// (<c>{{appLinks}}</c>) and <see cref="BaseEmailService"/> fills it into every mail it sends, so no
/// service builds the band, none keeps a copy of it, and none can send a mail without it.
/// </summary>
/// <remarks>
/// Text and links only, every style on the element it belongs to: mail clients block images until the
/// reader asks, and one that strips the style block must still show two links. The band names its own
/// background and each text its own colour, so none of them depends on the card behind it in a dark
/// mail client. The buttons are outlined, so the mail's solid action button stays the one thing to press.
/// </remarks>
public static class AppLinksEmailBlock
{
    public const string TemplateKey = "appLinks";
    public const string Placeholder = "{{" + TemplateKey + "}}";
    public const string Heading = "Get the Spike app";
    public const string AppStoreUrl = "https://apps.apple.com/app/id6792373783";
    public const string GooglePlayUrl = "https://play.google.com/store/apps/details?id=app.volleyspike.mobile";

    // The buttons are filled with the band's own colour, so only their outline shows.
    private const string BandBackground = "#303030";
    private const string BandStyle = $"background-color:{BandBackground};border-top:1px solid #444444;padding:24px 24px 20px;text-align:center";
    private const string HeadingStyle = "margin:0 0 14px;color:#fbfbfb;font-size:15px;font-weight:600";
    private const string LinkStyle = $"display:inline-block;margin:0 6px 8px;padding:10px 22px;border:1px solid #f97316;border-radius:10px;background-color:{BandBackground};color:#fb923c;font-size:14px;font-weight:600;text-decoration:none";

    public const string Html = $"""
        <div style="{BandStyle}">
        <p style="{HeadingStyle}">{Heading}</p>
        <a href="{AppStoreUrl}" style="{LinkStyle}">App Store</a>
        <a href="{GooglePlayUrl}" style="{LinkStyle}">Google Play</a>
        </div>
        """;

    /// <summary>
    /// The template body with the band where it names <see cref="Placeholder"/>. The mail service does
    /// this before it fills anything a caller sent: a squad named "{{appLinks}}" is then typed text in
    /// a finished mail, not a second place for the band, and a caller's own <see cref="TemplateKey"/>
    /// finds no placeholder left to fill.
    /// </summary>
    public static string FillPlaceholderIn(string templateBody) => templateBody.Replace(Placeholder, Html);
}
