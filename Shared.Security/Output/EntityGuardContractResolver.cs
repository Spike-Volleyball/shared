using Newtonsoft.Json.Serialization;

namespace Shared.Security.Output;

/// <summary>The <see cref="EntityGuard"/> for Newtonsoft, wrapped around a service's own resolver.</summary>
public sealed class EntityGuardContractResolver(IContractResolver inner) : IContractResolver
{
    public JsonContract ResolveContract(Type type) =>
        EntityGuard.IsEntity(type) ? throw new EntityExposureException(type) : inner.ResolveContract(type);
}
