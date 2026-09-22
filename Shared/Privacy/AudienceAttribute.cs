namespace Shared.Privacy;

[Flags]
public enum PersonalDataAudience
{
    /// <summary>The person themselves.</summary>
    Self = 1,

    /// <summary>A guardian with standing over the person.</summary>
    Guardian = 2,

    /// <summary>Staff of a club the person belongs to, as clubs' contact rule allows.</summary>
    ClubStaff = 4,

    /// <summary>The organiser of an event the person takes part in.</summary>
    Organiser = 8,
}

/// <summary>Who a DTO carrying <see cref="PersonalDataAttribute"/> properties is built for.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class AudienceAttribute(PersonalDataAudience audience) : Attribute
{
    public PersonalDataAudience Audience { get; } = audience;
}
