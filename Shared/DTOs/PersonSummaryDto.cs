using Shared.Models;

namespace Shared.DTOs;

/// <summary>
/// The one shape for a person inside another resource - a participant, a captain, an author, a
/// member. Name and photo only: contact details and age live in audience-specific DTOs marked
/// <see cref="Privacy.PersonalDataAttribute"/>, never in a summary (SPI-6446).
/// </summary>
/// <remarks>The keys are the ones web and mobile already read on every service's person DTO.</remarks>
public sealed record PersonSummaryDto
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public string? Surname { get; init; }
    public string? ImageUrl { get; init; }
    public string? ImageThumbHash { get; init; }

    public static PersonSummaryDto From(UserProfile profile) => new()
    {
        Id = profile.Id,
        Name = profile.Name,
        Surname = profile.Surname,
        ImageUrl = profile.ImageUrl,
        ImageThumbHash = profile.ImageThumbHash,
    };
}
