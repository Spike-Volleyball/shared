using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Shared.Models;

namespace Shared.Security.Output;

/// <summary>
/// Refuses to serialise an entity, whichever serializer is asked. An entity carries whatever EF
/// happened to load - people's email, phone and date of birth included - so the only safe shape to
/// send is a DTO built for the reader. Responses leaked exactly this way until 2026-09 (SPI-6446).
/// </summary>
/// <remarks>
/// Requests are guarded too: an entity bound from a body is a client choosing every column.
/// </remarks>
public static class EntityGuard
{
    private const string DomainAssemblySuffix = ".Domain";

    private static readonly ConcurrentDictionary<Type, bool> Verdicts = new();

    /// <summary>
    /// A class implementing <see cref="IEntity{TKey}"/>, or any class from a <c>*.Domain</c>
    /// assembly. Enums and structs from those assemblies are values and pass. An array is only a
    /// container - it reports its element's assembly - so it passes and its elements are judged.
    /// </summary>
    public static bool IsEntity(Type type) => Verdicts.GetOrAdd(type, static t =>
    {
        var candidate = Nullable.GetUnderlyingType(t) ?? t;
        if (!candidate.IsClass || candidate.IsArray || candidate == typeof(string))
            return false;

        return candidate.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>))
            || candidate.Assembly.GetName().Name?.EndsWith(DomainAssemblySuffix, StringComparison.Ordinal) == true;
    });

    /// <summary>For System.Text.Json callers - SignalR hubs, and services on STJ.</summary>
    public static void Guard(JsonSerializerOptions options) =>
        options.TypeInfoResolver = (options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver())
            .WithAddedModifier(RefuseEntities);

    private static void RefuseEntities(JsonTypeInfo typeInfo)
    {
        if (IsEntity(typeInfo.Type))
            throw new EntityExposureException(typeInfo.Type);
    }
}
