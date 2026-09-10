using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using MockQueryable;
using NSubstitute;
using Shared.DataAccess.Repositories.Interfaces;
using Shared.Enums;
using Shared.Guardian.Interfaces;
using Shared.Guardian.Models;
using Shared.Guardian.Services;
using Shared.Models;
using Shared.Services.Services;

namespace Shared.Tests.Guardian;

/// <summary>
/// The roster form of the ward-side reconcile. One people list used to cost one marker read, one
/// source call and one save PER MEMBER; these pin the shape that replaced it — every marker read
/// up front, one source call for the cold wards, one save — and that the rows it leaves are the
/// ones the per-ward form would have left.
/// </summary>
[TestFixture]
[Category("Unit")]
public class GuardianLinkServiceWardBatchTests
{
    private const string WardMarkerPrefix = "guardian-links-ward-verified:";
    private const string WardForcedMarkerPrefix = "guardian-links-ward-forced:";

    private static readonly Guid FirstWard = Guid.NewGuid();
    private static readonly Guid SecondWard = Guid.NewGuid();
    private static readonly Guid ThirdWard = Guid.NewGuid();
    private static readonly Guid GuardianId = Guid.NewGuid();
    private static readonly Guid OtherGuardianId = Guid.NewGuid();
    private static readonly DateTimeOffset FrozenNow = new(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private readonly Dictionary<string, byte[]> _cacheStore = new();

    private IRepository<GuardianLink> _linkRepository = null!;
    private IGuardianLinkSource _source = null!;
    private IDistributedCache _cache = null!;
    private RecordingLogger<GuardianLinkService> _logger = null!;
    private GuardianLinkService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _cacheStore.Clear();
        _linkRepository = Substitute.For<IRepository<GuardianLink>>();
        _source = Substitute.For<IGuardianLinkSource>();
        _cache = Substitute.For<IDistributedCache>();
        _logger = new RecordingLogger<GuardianLinkService>();

        var userProfileRepository = Substitute.For<IRepository<UserProfile>>();
        userProfileRepository.Query().Returns(new List<UserProfile>().BuildMock());

        GivenLinks();
        GivenRemoteGuardians(new Dictionary<Guid, IReadOnlyList<Guid>>());
        StubCacheAsAStore();

        _sut = new GuardianLinkService(_linkRepository, userProfileRepository, _source, _cache,
            new AgeTierService(new FakeTimeProvider(FrozenNow)), _logger);
    }

    private void StubCacheAsAStore()
    {
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(
                _cacheStore.TryGetValue(ci.ArgAt<string>(0), out var value) ? value : null));
        _cache.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                _cacheStore[ci.ArgAt<string>(0)] = ci.ArgAt<byte[]>(1);
                return Task.CompletedTask;
            });
    }

    private void GivenLinks(params GuardianLink[] links) =>
        _linkRepository.Query().Returns(links.ToList().BuildMock());

    private void GivenRemoteGuardians(IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> guardiansByWard) =>
        _source.GetGuardiansForMinorsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(guardiansByWard);

    private void GivenWarmMarker(Guid wardId) => _cacheStore[WardMarkerPrefix + wardId] = "1"u8.ToArray();

    private static GuardianLink Link(Guid guardianId, Guid wardId, GuardianPermission permissions) =>
        new() { GuardianUserId = guardianId, WardUserId = wardId, Permissions = permissions };

    private IReadOnlyList<GuardianLink> AddedLinks() => _linkRepository.ReceivedCalls()
        .Where(c => c.GetMethodInfo().Name == nameof(IRepository<GuardianLink>.Add))
        .Select(c => (GuardianLink)c.GetArguments()[0]!)
        .ToList();

    private Task AssertAskedOnceFor(params Guid[] wardIds) =>
        _source.Received(1).GetGuardiansForMinorsAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.ToHashSet().SetEquals(wardIds)),
            Arg.Any<CancellationToken>());

    [Test]
    public async Task EnsureWardsFreshAsync_EveryMarkerCold_OneSourceCallAndOneSaveForTheRoster()
    {
        // Arrange
        GivenRemoteGuardians(new Dictionary<Guid, IReadOnlyList<Guid>>
        {
            [FirstWard] = [GuardianId],
            [SecondWard] = [GuardianId, OtherGuardianId],
            [ThirdWard] = []
        });

        // Act
        await _sut.EnsureWardsFreshAsync([FirstWard, SecondWard, ThirdWard]);

        // Assert
        await AssertAskedOnceFor(FirstWard, SecondWard, ThirdWard);
        await _source.DidNotReceive().GetGuardiansForMinorAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _linkRepository.Received(1).SaveChangesAsync();
        AddedLinks().Select(l => (l.GuardianUserId, l.WardUserId)).Should().BeEquivalentTo(new[]
        {
            (GuardianId, FirstWard), (GuardianId, SecondWard), (OtherGuardianId, SecondWard)
        });
    }

    [Test]
    public async Task EnsureWardsFreshAsync_EveryMarkerCold_ReadsEveryMarkerBeforeAskingTheSource()
    {
        // Arrange
        var sourceAskedAfterReads = false;
        _source.GetGuardiansForMinorsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                sourceAskedAfterReads = _cache.ReceivedCalls()
                    .Count(c => c.GetMethodInfo().Name == nameof(IDistributedCache.GetAsync)) == 3;
                return new Dictionary<Guid, IReadOnlyList<Guid>>();
            });

        // Act
        await _sut.EnsureWardsFreshAsync([FirstWard, SecondWard, ThirdWard]);

        // Assert
        sourceAskedAfterReads.Should().BeTrue();
        await _cache.Received(3).GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EnsureWardsFreshAsync_EveryMarkerCold_MarksEveryWardVerifiedForAnHour()
    {
        // Arrange
        // Act
        await _sut.EnsureWardsFreshAsync([FirstWard, SecondWard]);

        // Assert
        foreach (var wardId in new[] { FirstWard, SecondWard })
        {
            await _cache.Received(1).SetAsync(WardMarkerPrefix + wardId, Arg.Any<byte[]>(),
                Arg.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == TimeSpan.FromHours(1)),
                Arg.Any<CancellationToken>());
            await _cache.Received(1).SetAsync(WardForcedMarkerPrefix + wardId, Arg.Any<byte[]>(),
                Arg.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == TimeSpan.FromSeconds(60)),
                Arg.Any<CancellationToken>());
        }
    }

    [Test]
    public async Task EnsureWardsFreshAsync_EveryMarkerWarm_NeverTouchesTheSourceOrTheDatabase()
    {
        // Arrange
        GivenWarmMarker(FirstWard);
        GivenWarmMarker(SecondWard);
        GivenWarmMarker(ThirdWard);

        // Act
        await _sut.EnsureWardsFreshAsync([FirstWard, SecondWard, ThirdWard]);

        // Assert
        await _source.DidNotReceive().GetGuardiansForMinorsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
        await _source.DidNotReceive().GetGuardiansForMinorAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        _linkRepository.DidNotReceive().Query();
        await _linkRepository.DidNotReceive().SaveChangesAsync();
        await _cache.DidNotReceive().SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EnsureWardsFreshAsync_SomeMarkersWarm_AsksOnlyForTheColdWards()
    {
        // Arrange
        GivenWarmMarker(SecondWard);

        // Act
        await _sut.EnsureWardsFreshAsync([FirstWard, SecondWard, ThirdWard]);

        // Assert
        await AssertAskedOnceFor(FirstWard, ThirdWard);
    }

    [Test]
    public async Task EnsureWardsFreshAsync_SameWardTwice_AsksOnce()
    {
        // Arrange
        // Act
        await _sut.EnsureWardsFreshAsync([FirstWard, FirstWard]);

        // Assert
        await AssertAskedOnceFor(FirstWard);
        await _cache.Received(1).GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EnsureWardsFreshAsync_NoWards_DoesNothing()
    {
        // Arrange
        // Act
        await _sut.EnsureWardsFreshAsync([]);

        // Assert
        await _cache.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _source.DidNotReceive().GetGuardiansForMinorsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// The per-ward form deletes every local link of a ward the source says has no guardians. A
    /// ward the batch answer does not mention at all is the same statement and gets the same
    /// treatment — silently keeping the rows would be the one way the batch could disagree.
    /// </summary>
    [Test]
    public async Task EnsureWardsFreshAsync_WardAbsentFromTheAnswer_RemovesItsLocalLinks()
    {
        // Arrange
        var stale = Link(GuardianId, FirstWard, GuardianPermission.View);
        GivenLinks(stale);
        GivenRemoteGuardians(new Dictionary<Guid, IReadOnlyList<Guid>> { [SecondWard] = [GuardianId] });

        // Act
        await _sut.EnsureWardsFreshAsync([FirstWard, SecondWard]);

        // Assert
        _linkRepository.Received(1).Delete(stale);
        AddedLinks().Should().ContainSingle(l => l.GuardianUserId == GuardianId && l.WardUserId == SecondWard);
    }

    [Test]
    public async Task EnsureWardsFreshAsync_ExistingGuardian_DoesNotDowngradePermissionsOrTier()
    {
        // Arrange
        var existing = new GuardianLink
        {
            GuardianUserId = GuardianId, WardUserId = FirstWard,
            Permissions = GuardianPermission.View | GuardianPermission.Pay, Tier = GuardianTier.Contact
        };
        GivenLinks(existing);
        GivenRemoteGuardians(new Dictionary<Guid, IReadOnlyList<Guid>> { [FirstWard] = [GuardianId] });

        // Act
        await _sut.EnsureWardsFreshAsync([FirstWard]);

        // Assert
        existing.Permissions.Should().Be(GuardianPermission.View | GuardianPermission.Pay);
        existing.Tier.Should().Be(GuardianTier.Contact);
        _linkRepository.DidNotReceive().Add(Arg.Any<GuardianLink>());
        _linkRepository.DidNotReceive().Update(Arg.Any<GuardianLink>());
        _linkRepository.DidNotReceive().Delete(Arg.Any<GuardianLink>());
    }

    [Test]
    public async Task EnsureWardsFreshAsync_OnlyLinksOfTheColdWardsAreConsidered()
    {
        // Arrange — a warm ward's rows are somebody else's business this time round.
        var warmWardsLink = Link(GuardianId, SecondWard, GuardianPermission.View);
        GivenLinks(warmWardsLink);
        GivenWarmMarker(SecondWard);

        // Act
        await _sut.EnsureWardsFreshAsync([FirstWard, SecondWard]);

        // Assert
        _linkRepository.DidNotReceive().Delete(warmWardsLink);
    }

    [Test]
    public async Task EnsureWardsFreshAsync_SourceThrows_DoesNotThrowAndMarksEveryColdWardForRetry()
    {
        // Arrange
        var existing = Link(GuardianId, FirstWard, GuardianPermission.Pay);
        GivenLinks(existing);
        _source.GetGuardiansForMinorsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>>(_ =>
                throw new InvalidOperationException("profiles unavailable"));

        // Act
        var act = () => _sut.EnsureWardsFreshAsync([FirstWard, SecondWard]);

        // Assert
        await act.Should().NotThrowAsync();
        _linkRepository.DidNotReceive().Delete(Arg.Any<GuardianLink>());
        _linkRepository.DidNotReceive().Add(Arg.Any<GuardianLink>());
        await _linkRepository.DidNotReceive().SaveChangesAsync();
        foreach (var wardId in new[] { FirstWard, SecondWard })
        {
            await _cache.Received(1).SetAsync(WardMarkerPrefix + wardId, Arg.Any<byte[]>(),
                Arg.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == TimeSpan.FromMinutes(5)),
                Arg.Any<CancellationToken>());
        }
    }

    [Test]
    public async Task EnsureWardsFreshAsync_LosesTheInsertRace_DetachesEveryStagedLinkQuietly()
    {
        // Arrange
        GivenRemoteGuardians(new Dictionary<Guid, IReadOnlyList<Guid>>
        {
            [FirstWard] = [GuardianId],
            [SecondWard] = [GuardianId]
        });
        _linkRepository.SaveChangesAsync().Returns<Task>(_ => throw GuardianLinkFailures.DuplicatePair());

        // Act
        await _sut.EnsureWardsFreshAsync([FirstWard, SecondWard]);

        // Assert
        _logger.Entries.Should().NotContain(e => e.Level >= LogLevel.Warning);
        _linkRepository.Received(1).Detach(Arg.Is<GuardianLink>(l => l.WardUserId == FirstWard));
        _linkRepository.Received(1).Detach(Arg.Is<GuardianLink>(l => l.WardUserId == SecondWard));
    }

    [Test]
    public async Task EnsureWardsFreshAsync_SaveFailsForAnotherReason_NamesTheActualFailure()
    {
        // Arrange
        GivenRemoteGuardians(new Dictionary<Guid, IReadOnlyList<Guid>> { [FirstWard] = [GuardianId] });
        _linkRepository.SaveChangesAsync().Returns<Task>(_ => throw new DbUpdateException("deadlock detected"));

        // Act
        await _sut.EnsureWardsFreshAsync([FirstWard]);

        // Assert
        var warning = _logger.Entries.Single(e => e.Level == LogLevel.Warning);
        warning.Message.Should().Contain(nameof(DbUpdateException));
        _linkRepository.Received(1).Detach(Arg.Is<GuardianLink>(l => l.WardUserId == FirstWard));
    }
}
