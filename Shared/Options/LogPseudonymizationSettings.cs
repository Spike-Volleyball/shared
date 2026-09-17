namespace Shared.Options;

public class LogPseudonymizationSettings
{
    public const string SectionName = "LogPseudonymization";

    /// <summary>
    /// Anything shorter can be brute-forced from a single address whose hash is known, and every
    /// other hash in the logs falls to a list of guesses after it.
    /// </summary>
    public const int MinimumKeyLength = 32;

    /// <summary>
    /// Secret for the keyed hash that stands in for an email address or a phone number in the
    /// logs. Every service needs the same value for one identifier to correlate across all of
    /// them. Unset or too short, the logs carry only a mask.
    /// </summary>
    public string? HmacKey { get; set; }

    public bool HasUsableKey => HmacKey?.Length >= MinimumKeyLength;
}
