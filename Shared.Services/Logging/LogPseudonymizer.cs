using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Shared.Options;

namespace Shared.Services.Logging;

/// <summary>
/// The hash is HMAC-SHA256 over the UTF-8 of the normalised value — an address trimmed and
/// lower-cased, a phone number cut down to its leading '+' and its digits — keyed with the UTF-8
/// of <see cref="LogPseudonymizationSettings.HmacKey"/> and cut to its first 16 hex digits, so
/// the value to search the logs for is
/// <c>printf '%s' "$NORMALISED" | openssl dgst -sha256 -hmac "$KEY" -r | cut -c1-16</c>.
/// Without a usable key there is no hash at all: an unkeyed one is reversed by hashing a list of
/// likely values.
/// </summary>
public sealed class LogPseudonymizer : ILogPseudonymizer
{
    private const int HashHexLength = 16;
    private const string Masked = "***";
    private const string NoValue = "(empty)";

    /// <summary>Below this the last two digits are too large a share of the number to show.</summary>
    private const int MinimumDigitsToShowTheEnd = 6;
    private const int VisibleTrailingDigits = 2;

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

    public string PseudonymizePhoneNumber(string? phoneNumber)
    {
        var trimmed = phoneNumber?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return NoValue;

        // Spacing, dashes and brackets vary with whoever typed the number; the digits do not.
        var digits = new string(trimmed.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length == 0)
            return Masked;

        var international = trimmed.StartsWith('+') ? "+" : string.Empty;
        var mask = digits.Length < MinimumDigitsToShowTheEnd
            ? Masked
            : $"{international}{Masked}{digits[^VisibleTrailingDigits..]}";
        return _key is null ? mask : $"{mask} hmac:{Hash(_key, international + digits)}";
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
