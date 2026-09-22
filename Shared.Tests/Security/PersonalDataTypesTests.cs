using System.Reflection;
using System.Reflection.Emit;
using FluentAssertions;
using Shared.DTOs;
using Shared.Models;
using Shared.Privacy;
using Shared.Testing.Security;

namespace Shared.Tests.Security;

[TestFixture]
[Category("Unit")]
public class PersonalDataTypesTests
{
    [Test]
    public void Unmarked_FindsAPersonalFieldCarriedWithoutSayingSo()
    {
        // Act & Assert
        PersonalDataTypes.Unmarked(Dtos.Assembly).Should().Equal("Probe.Dtos.RosterRow.Email");
    }

    [Test]
    public void WithoutAudience_FindsAMarkedTypeThatNamesNoReader()
    {
        // Act & Assert
        PersonalDataTypes.WithoutAudience(Dtos.Assembly).Should().Equal("Probe.Dtos.ContactCard");
    }

    [Test]
    public void PersonSummary_FromAProfile_CarriesNameAndPhotoOnly()
    {
        // Arrange
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(), Name = "Marta", Surname = "Kovalenko", ImageUrl = "https://cdn/avatar.png",
            Email = "marta@test.com", PhoneNumber = "+44", DateOfBirth = new DateTime(2011, 3, 14),
        };

        // Act
        var summary = PersonSummaryDto.From(profile);

        // Assert
        summary.Should().BeEquivalentTo(new { profile.Id, profile.Name, profile.Surname, profile.ImageUrl });
        typeof(PersonSummaryDto).GetProperties().Select(p => p.Name)
            .Should().NotContain(["Email", "PhoneNumber", "DateOfBirth"]);
    }

    /// <summary>
    /// A DTO assembly with one unmarked personal field, one marked type without an audience and one
    /// done properly - built at run time so the checks see exactly these three.
    /// </summary>
    private static class Dtos
    {
        public static readonly Assembly Assembly;

        static Dtos()
        {
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Probe.Dtos"), AssemblyBuilderAccess.Run);
            var module = assembly.DefineDynamicModule("Probe.Dtos");
            Define(module, "Probe.Dtos.RosterRow", markEmail: false, audience: false);
            Define(module, "Probe.Dtos.ContactCard", markEmail: true, audience: false);
            Define(module, "Probe.Dtos.MemberContact", markEmail: true, audience: true);
            Assembly = assembly;
        }

        private static void Define(ModuleBuilder module, string name, bool markEmail, bool audience)
        {
            var type = module.DefineType(name, TypeAttributes.Public | TypeAttributes.Class);
            if (audience)
                type.SetCustomAttribute(new CustomAttributeBuilder(
                    typeof(AudienceAttribute).GetConstructors()[0], [PersonalDataAudience.Self]));

            var field = type.DefineField("_email", typeof(string), FieldAttributes.Private);
            var property = type.DefineProperty("Email", PropertyAttributes.None, typeof(string), null);
            var getter = type.DefineMethod("get_Email",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(string), Type.EmptyTypes);
            var il = getter.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);
            property.SetGetMethod(getter);
            if (markEmail)
                property.SetCustomAttribute(new CustomAttributeBuilder(
                    typeof(PersonalDataAttribute).GetConstructor(Type.EmptyTypes)!, []));

            type.CreateType();
        }
    }
}
