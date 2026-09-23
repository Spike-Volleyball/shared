using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Shared.Models;
using Shared.Security.Output;

namespace Shared.Tests.Security;

[TestFixture]
[Category("Unit")]
public class EntityGuardTests
{
    public sealed class Profile : BaseEntity
    {
        public string? Email { get; set; }
    }

    public sealed record ProfileDto(Guid Id, string Name);

    public sealed record Envelope(ProfileDto Summary, Profile Leak);

    private static readonly JsonSerializerSettings GuardedNewtonsoft = new()
    {
        ContractResolver = new EntityGuardContractResolver(new CamelCasePropertyNamesContractResolver()),
    };

    [Test]
    public void IsEntity_AClassImplementingIEntity_IsAnEntity()
    {
        // Act & Assert
        EntityGuard.IsEntity(typeof(Profile)).Should().BeTrue();
    }

    [Test]
    public void IsEntity_ADto_IsNot()
    {
        // Act & Assert
        EntityGuard.IsEntity(typeof(ProfileDto)).Should().BeFalse();
    }

    [Test]
    public void IsEntity_AClassFromADomainAssembly_IsAnEntity()
    {
        // Act & Assert
        EntityGuard.IsEntity(DomainAssembly.Class).Should().BeTrue();
    }

    [Test]
    public void IsEntity_AnEnumFromADomainAssembly_IsAValue()
    {
        // Act & Assert
        EntityGuard.IsEntity(DomainAssembly.Enum).Should().BeFalse();
    }

    [Test]
    public void Newtonsoft_AnEntity_IsRefused()
    {
        // Act
        var act = () => JsonConvert.SerializeObject(new Profile { Email = "someone@test.com" }, GuardedNewtonsoft);

        // Assert
        act.Should().Throw<EntityExposureException>().Which.EntityType.Should().Be(typeof(Profile));
    }

    [Test]
    public void Newtonsoft_AnEntityInsideADto_IsRefused()
    {
        // Act
        var act = () => JsonConvert.SerializeObject(
            new Envelope(new ProfileDto(Guid.NewGuid(), "Marta"), new Profile()), GuardedNewtonsoft);

        // Assert
        act.Should().Throw<EntityExposureException>();
    }

    [Test]
    public void Newtonsoft_ADto_IsWrittenByTheServicesOwnResolver()
    {
        // Act
        var json = JsonConvert.SerializeObject(new ProfileDto(Guid.Empty, "Marta"), GuardedNewtonsoft);

        // Assert
        json.Should().Contain("\"name\":\"Marta\"");
    }

    [Test]
    public void Newtonsoft_ARequestBodyBoundToAnEntity_IsRefused()
    {
        // Act
        var act = () => JsonConvert.DeserializeObject<Profile>("{\"email\":\"x@test.com\"}", GuardedNewtonsoft);

        // Assert
        act.Should().Throw<EntityExposureException>();
    }

    [Test]
    public void SystemTextJson_AListOfEntities_IsRefused()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        EntityGuard.Guard(options);

        // Act
        var act = () => System.Text.Json.JsonSerializer.Serialize(new List<Profile> { new() }, options);

        // Assert
        act.Should().Throw<EntityExposureException>();
    }

    [Test]
    public void SystemTextJson_ADto_IsWritten()
    {
        // Arrange
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        EntityGuard.Guard(options);

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(new ProfileDto(Guid.Empty, "Marta"), options);

        // Assert
        json.Should().Contain("\"name\":\"Marta\"");
    }

    /// <summary>A class and an enum from an assembly named like a service's domain project.</summary>
    private static class DomainAssembly
    {
        public static readonly Type Class;
        public static readonly Type Enum;

        static DomainAssembly()
        {
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Probe.Domain"), AssemblyBuilderAccess.Run);
            var module = assembly.DefineDynamicModule("Probe.Domain");
            Class = module.DefineType("Probe.Domain.Venue", TypeAttributes.Public | TypeAttributes.Class).CreateType();
            var surface = module.DefineEnum("Probe.Domain.Surface", TypeAttributes.Public, typeof(int));
            surface.DefineLiteral("Indoor", 0);
            Enum = surface.CreateType();
        }
    }
}
