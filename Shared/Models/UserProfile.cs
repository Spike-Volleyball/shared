namespace Shared.Models;

public class UserProfile : BaseEntity
{
    public const string DeletedUserName = "Deleted User";

    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageThumbHash { get; set; }
    public string? Email { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public bool IsActive { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsEmailVerified { get; set; }

    public string FullName => $"{Name} {Surname}".Trim();

    /// <summary>
    /// What an erased account leaves in a copy of its profile: a row other records can still point
    /// at, under a name that says the person is gone, and nothing that identifies them.
    /// </summary>
    public void Anonymize()
    {
        Name = DeletedUserName;
        Surname = string.Empty;
        ImageUrl = null;
        ImageThumbHash = null;
        Email = null;
        PhoneNumber = null;
        DateOfBirth = null;
        IsActive = false;
        IsEmailVerified = false;
    }
}
