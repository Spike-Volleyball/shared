using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Shared.Options;

namespace Shared.Services.Logging;

/// <summary>
/// The hash is HMAC-SHA256 over the UTF-8 of the trimmed, lower-cased address, keyed with the
/// UTF-8 of <see cref="LogPseudonymizationSettings.HmacKey"/> and cut to its first 16 hex digits,
/// so the value to search the logs for is
/// <c>printf '%s' "$ADDRESS" | openssl dgst -sha256 -hmac "$KEY" -r | cut -c1-16</c>.
/// Without a usable key there is no hash at all: an unkeyed one is reversed by hashing a list of
/// likely addresses.
/// </summary>
public sealed class LogPseudonymizer : ILogPseudonymizer
{
    private const int HashHexLength = 16;
    private const string Masked = "***";
    private const string NoValue = "(empty)";

    private readonly byte[]? _key;

    public LogPseudonymizer(IOptions<LogPseudonymizationSettings> options)
    {
        var settings = options.Value;
        _key = settings.HasUsableKey ? Encoding.UTF8.GetBytes(settings.HmacKey!) : null;
    }

    public string PseudonymizeEmail(string? email)
    {
        var normalized = email?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalized))
            return NoValue;

        var mask = MaskEmail(normalized);
        return _key is null ? mask : $"{mask} hmac:{Hash(_key, normalized)}";
    }

    private static string Hash(byte[] key, string value) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(value)))[..HashHexLength];

    private static string MaskEmail(string email)
    {
        // The last '@' separates the domain: a quoted local part may itself contain one.
        var at = email.LastIndexOf('@');
        if (at < 0)
            return Masked;

        var domain = email[(at + 1)..];
        var lastDot = domain.LastIndexOf('.');
        var topLevelDomain = lastDot > 0 ? domain[lastDot..] : string.Empty;

        return $"{MaskPart(email[..at])}@{MaskPart(domain)}{topLevelDomain}";
    }

    private static string MaskPart(string part)
    {
        if (part.Length == 0)
            return Masked;

        // A rune, not a char: a local part may open with a character outside the BMP, and half of
        // a surrogate pair is not something a log sink can render.
        Rune.DecodeFromUtf16(part, out var first, out _);
        return $"{first}{Masked}";
    }
}
