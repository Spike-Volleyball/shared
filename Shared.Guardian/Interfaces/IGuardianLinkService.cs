using Shared.Enums;

namespace Shared.Guardian.Interfaces;

public interface IGuardianLinkService
{
    Task UpsertAsync(Guid guardianId, Guid wardId, GuardianPermission permissions,
                     GuardianTier tier = GuardianTier.Guardian);
    Task RemoveAsync(Guid guardianId, Guid wardId);
    Task RemoveAllForWardAsync(Guid wardId);
    Task RemoveAllForUserAsync(Guid userId);

    /// <summary>
    /// Wards from the link rows alone. The link IS profiles-service's statement that this
    /// ward is a minor under guardianship, and profiles ends it at 18 (AdultTransitionJob) —
    /// re-deriving minority from a local DateOfBirth is what silently killed oversight for
    /// every null-DOB replica row in August. Access derivation uses THIS.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetWardIdsAsync(Guid guardianId, GuardianPermission required = GuardianPermission.None);

    /// <summary>
    /// GetWardIdsAsync additionally filtered to wards the LOCAL UserProfiles replica says are
    /// minors. Only messages-service's chat-oversight surface uses this — it preserves that
    /// service's existing semantics exactly. New callers should use GetWardIdsAsync.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetMinorWardIdsAsync(Guid guardianId, GuardianPermission required = GuardianPermission.None);

    /// <param name="force">
    /// Skip the one-hour success marker. The miss path must force: a warm marker otherwise
    /// hides a dropped grant for up to an hour. Forced calls are rate-limited to one per
    /// 60 s per user by a separate marker.
    /// </param>
    Task EnsureFreshAsync(Guid userId, bool force = false);

    /// <summary>
    /// The guardians of one ward, from the local replica alone. The reverse of GetWardIdsAsync;
    /// the WardUserId index already exists (RemoveAllForWardAsync uses it).
    /// </summary>
    Task<IReadOnlyList<Guid>> GetGuardianIdsForWardAsync(Guid wardUserId);

    /// <param name="force">Skip the one-hour success marker, as EnsureFreshAsync does.</param>
    /// <summary>
    /// Reconciles the links of ONE WARD against the source. EnsureFreshAsync is guardian-keyed and
    /// can therefore only ever heal a guardian who has made a request; a people list has to find
    /// guardians who have never opened the app, and the only id it holds is the ward's.
    /// </summary>
    Task EnsureWardFreshAsync(Guid wardUserId, bool force = false);

    /// <summary>
    /// EnsureWardFreshAsync for a whole roster, unforced: the hourly marker is read for every ward
    /// at once, the wards whose marker is cold go to the source in ONE call, and their links are
    /// reconciled under one SaveChangesAsync. The resulting rows and markers are exactly what one
    /// EnsureWardFreshAsync per ward would have left behind.
    /// </summary>
    Task EnsureWardsFreshAsync(IReadOnlyCollection<Guid> wardUserIds);
}
