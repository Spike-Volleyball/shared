using Shared.Enums;

namespace Shared.Models.Jwt;

public class JwtPayloadDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public required string AccessToken { get; init; }
    public AgeTier? AgeTier { get; set; }
    public bool HasRequiredConsent { get; set; } = true;
    public DateTime? ConsentLastModified { get; set; }
    public int SessionVersion { get; set; } = SessionVersions.Initial;
}
