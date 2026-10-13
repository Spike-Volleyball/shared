namespace Shared.Services.FileStorage;

public static class ContentDispositionValue
{
    /// <summary>
    /// Builds an RFC 6266 "inline" Content-Disposition value so browsers render the
    /// file in-tab and still get a sensible name on save. Emits an ASCII-safe
    /// filename plus an RFC 5987 UTF-8 filename* variant when the name needs one.
    /// </summary>
    public static string Inline(string? fileName) => Build("inline", fileName);

    /// <summary>
    /// Same as <see cref="Inline"/> but tells browsers to download the file instead of rendering it.
    /// </summary>
    public static string Attachment(string? fileName) => Build("attachment", fileName);

    // An SVG opened directly in a tab is a document that runs its own scripts on the bucket's
    // origin, while <img> never runs them and ignores Content-Disposition, so an SVG is stored
    // to download when navigated to and still draws wherever it is embedded.
    public static string For(string contentType, string? fileName) =>
        string.Equals(contentType, ContentTypes.Svg, StringComparison.OrdinalIgnoreCase)
            ? Attachment(fileName)
            : Inline(fileName);

    private static string Build(string disposition, string? fileName)
    {
        var sanitized = fileName == null
            ? string.Empty
            : new string(fileName.Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (sanitized.Length == 0)
            return disposition;

        var fallback = new string(sanitized.Select(c => c is '"' or '\\' || c > '\x7e' ? '_' : c).ToArray());
        var value = $"{disposition}; filename=\"{fallback}\"";
        return fallback == sanitized
            ? value
            : $"{value}; filename*=UTF-8''{Uri.EscapeDataString(sanitized)}";
    }
}
