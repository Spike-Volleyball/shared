using Shared.Enums;

namespace Shared.Guardian.Interfaces;

/// <summary>
/// The remote authority on guardianship — profiles-service, as every service adopting this
/// replica already reaches it. Each service adapts its own gRPC client to this; shared never
/// owns a gRPC channel.
/// </summary>
public interface IGuardianLinkSource
{
    /// <summary>Throws on transport failure — the caller decides what a failure means.</summary>
    Task<IReadOnlyList<Guid>> GetMinorsForGuardianAsync(Guid guardianId, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> GetGuardiansForMinorAsync(Guid minorId, CancellationToken ct = default);

    /// <summary>
    /// GetGuardiansForMinorAsync for many minors in one round trip. Every requested minor is a key
    /// of the result; a minor with no guardians maps to an empty list. Throws on transport
    /// failure, like the single-id form. The default fans out one call per minor so an adapter
    /// that never seeds a roster keeps compiling; anything that does should override it with the
    /// batch RPC.
    /// </summary>
    async Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> GetGuardiansForMinorsAsync(
        IReadOnlyCollection<Guid> minorIds, CancellationToken ct = default)
    {
        var result = new Dictionary<Guid, IReadOnlyList<Guid>>();
        foreach (var minorId in minorIds)
            result[minorId] = await GetGuardiansForMinorAsync(minorId, ct);
        return result;
    }

    /// <summary>Null means "could not ask" — never "no access". A null must leave an existing link alone.</summary>
    Task<GuardianLinkAccess?> CheckGuardianAccessAsync(Guid guardianId, Guid minorId, CancellationToken ct = default);
}

public sealed record GuardianLinkAccess(bool HasAccess, GuardianPermission Permissions,
                                        GuardianTier Tier = GuardianTier.Guardian);
