namespace Shared.Security.PublicReads;

/// <summary>The per-visitor limit on signed-out reads of public endpoints.</summary>
public sealed class PublicReadSettings
{
    public const string SectionName = "PublicReads";

    /// <summary>
    /// What the web's server sends in <see cref="PublicReadLimit.RendererHeader"/> on the reads it makes to
    /// render a page. Unset, nothing is limited.
    /// </summary>
    public string? RendererSecret { get; set; }

    /// <summary>Reads one visitor may make in a <see cref="Window"/>. A person reading by hand never comes near it.</summary>
    public int PermitLimit { get; set; } = 120;

    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}
