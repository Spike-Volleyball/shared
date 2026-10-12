namespace Shared.Security.PublicReads;

/// <summary>The per-visitor limit on signed-out writes to public endpoints.</summary>
public sealed class PublicWriteSettings
{
    public const string SectionName = "PublicWrites";

    /// <summary>
    /// Writes one visitor may make in a <see cref="Window"/>. Several families registering from one tablet at a
    /// taster session stay under it; a script does not.
    /// </summary>
    public int PermitLimit { get; set; } = 20;

    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(10);
}
