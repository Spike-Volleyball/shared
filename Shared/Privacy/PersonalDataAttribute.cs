namespace Shared.Privacy;

/// <summary>
/// A DTO property holding someone's contact details, age or address. Its type must say who it is
/// for with <see cref="AudienceAttribute"/>; the architecture test finds any such property left
/// unmarked by its name.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PersonalDataAttribute : Attribute;
