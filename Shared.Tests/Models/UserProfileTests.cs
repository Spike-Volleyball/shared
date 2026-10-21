using FluentAssertions;
using Shared.Models;

namespace Shared.Tests.Models;

[TestFixture]
[Category("Unit")]
public class UserProfileTests
{
    [Test]
    public void Anonymize_LeavesNothingThatIdentifiesThePerson()
    {
        // Arrange
        var profile = new UserProfile
        {
            Name = "Lucia",
            Surname = "Moreno",
            ImageUrl = "https://cdn.example/lucia.png",
            ImageThumbHash = "lucia-thumb",
            Email = "lucia@example.com",
            PhoneNumber = "+447700900123",
            DateOfBirth = new DateTime(2008, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true,
            IsEmailVerified = true
        };
        var id = profile.Id;

        // Act
        profile.Anonymize();

        // Assert
        profile.Should().BeEquivalentTo(new
        {
            Id = id,
            Name = UserProfile.DeletedUserName,
            Surname = "",
            ImageUrl = (string?)null,
            ImageThumbHash = (string?)null,
            Email = (string?)null,
            PhoneNumber = (string?)null,
            DateOfBirth = (DateTime?)null,
            IsActive = false,
            IsEmailVerified = false
        });
    }
}
