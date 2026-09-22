using System.Reflection;
using Shared.Privacy;

namespace Shared.Testing.Security;

/// <summary>
/// Finds personal data a DTO carries without saying so, in the assembly that holds a service's DTOs.
/// Names are the net: a property called Email is personal data whatever its type says.
/// </summary>
public static class PersonalDataTypes
{
    private static readonly string[] PersonalNames =
    [
        "Email", "ContactEmail", "PhoneNumber", "Phone", "ContactPhone", "DateOfBirth",
        "Address", "AddressLine1", "AddressLine2", "PostCode", "Postcode", "PostalCode",
    ];

    /// <summary>Properties named like personal data that are not marked [PersonalData].</summary>
    public static IReadOnlyList<string> Unmarked(Assembly dtoAssembly) =>
        Dtos(dtoAssembly)
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => PersonalNames.Contains(p.Name) && p.GetCustomAttribute<PersonalDataAttribute>() is null)
                .Select(p => $"{t.FullName}.{p.Name}"))
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>Types carrying [PersonalData] properties that do not declare their [Audience].</summary>
    public static IReadOnlyList<string> WithoutAudience(Assembly dtoAssembly) =>
        Dtos(dtoAssembly)
            .Where(t => t.GetProperties().Any(p => p.GetCustomAttribute<PersonalDataAttribute>() is not null)
                        && t.GetCustomAttribute<AudienceAttribute>() is null)
            .Select(t => t.FullName!)
            .Order(StringComparer.Ordinal)
            .ToList();

    private static IEnumerable<Type> Dtos(Assembly assembly) =>
        assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false } && !t.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute)));
}
