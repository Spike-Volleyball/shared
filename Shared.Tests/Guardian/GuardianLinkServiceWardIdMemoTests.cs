using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
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
/// One request asks for a guardian's ward set several times — before and after a forced reconcile
/// that usually finds its 60-second marker warm and changes nothing. The service is scoped, so the
/// answer is remembered for the scope and forgotten the moment this service writes a link.
/// </summary>
[TestFixture]
[Category("Unit")]
public class GuardianLinkServiceWardIdMemoTests
{
    private static readonly Guid GuardianId = Guid.NewGuid();
    private static readonly Guid WardId = Guid.NewGuid();
    private static readonly DateTimeOffset FrozenNow = new(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private IRepository<GuardianLink> _linkRepository = null!;
    private IGuardianLinkSource _source = null!;
    private IDistributedCache _cache = null!;
    private GuardianLinkService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _linkRepository = Substitute.For<IRepository<GuardianLink>>();
        _source = Substitute.For<IGuardianLinkSource>();
        _cache = Substitute.For<IDistributedCache>();
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var userProfileRepository = Substitute.For<IRepository<UserProfile>>();
        userProfileRepository.Query().Returns(new List<UserProfile>().BuildMock());

        GivenLinks();

        _sut = new GuardianLinkService(_linkRepository, userProfileRepository, _source, _cache,
            new AgeTierService(new FakeTimeProvider(FrozenNow)), new RecordingLogger<GuardianLinkService>());
    }

    private void GivenLinks(params GuardianLink[] links) =>
        _linkRepository.Query().Returns(links.ToList().BuildMock());

    private static GuardianLink Link(Guid guardianId, Guid wardId) =>
        new() { GuardianUserId = guardianId, WardUserId = wardId, Permissions = GuardianPermission.View };

    [Test]
    public async Task GetWardIdsAsync_AskedTwiceInOneScope_QueriesOnce()
    {
        // Arrange
        GivenLinks(Link(GuardianId, WardId));

        // Act
        var first = await _sut.GetWardIdsAsync(GuardianId);
        var second = await _sut.GetWardIdsAsync(GuardianId);

        // Assert
        first.Should().Equal(WardId);
        second.Should().Equal(WardId);
        _linkRepository.Received(1).Query();
    }

    [Test]
    public async Task GetWardIdsAsync_DifferentRequiredPermission_IsADifferentQuestion()
    {
        // Arrange
        GivenLinks(Link(GuardianId, WardId));

        // Act
        var any = await _sut.GetWardIdsAsync(GuardianId);
        var pay = await _sut.GetWardIdsAsync(GuardianId, GuardianPermission.Pay);

        // Assert
        any.Should().Equal(WardId);
        pay.Should().BeEmpty();
        _linkRepository.Received(2).Query();
    }

    [Test]
    public async Task GetWardIdsAsync_AfterAWriteThroughTheService_QueriesAgain()
    {
        // Arrange — the rows change underneath the scope; only a write through the service
        // tells it to look again.
        var otherWard = Guid.NewGuid();
        var before = await _sut.GetWardIdsAsync(GuardianId);
        GivenLinks(Link(GuardianId, otherWard));

        // Act
        var remembered = await _sut.GetWardIdsAsync(GuardianId);
        await _sut.UpsertAsync(GuardianId, WardId, GuardianPermission.View);
        var after = await _sut.GetWardIdsAsync(GuardianId);

        // Assert
        before.Should().BeEmpty();
        remembered.Should().BeEmpty();
        after.Should().Equal(otherWard);
        await _linkRepository.Received(1).SaveChangesAsync();
    }

    /// <summary>
    /// The forced reconcile a request runs when it sees no wards: with its 60-second marker warm
    /// it does nothing, and the re-read that follows must not cost a second query.
    /// </summary>
    [Test]
    public async Task GetWardIdsAsync_AroundAReconcileThatWasSkipped_QueriesOnce()
    {
        // Arrange
        _cache.GetAsync("guardian-links-forced:" + GuardianId, Arg.Any<CancellationToken>())
            .Returns("1"u8.ToArray());

        // Act
        await _sut.GetWardIdsAsync(GuardianId);
        await _sut.EnsureFreshAsync(GuardianId, force: true);
        await _sut.GetWardIdsAsync(GuardianId);

        // Assert
        _linkRepository.Received(1).Query();
        await _source.DidNotReceive().GetMinorsForGuardianAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetWardIdsAsync_AroundAReconcileThatRan_QueriesAgain()
    {
        // Arrange
        _source.GetMinorsForGuardianAsync(GuardianId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Guid>)new List<Guid>());

        // Act
        await _sut.GetWardIdsAsync(GuardianId);
        await _sut.EnsureFreshAsync(GuardianId, force: true);
        await _sut.GetWardIdsAsync(GuardianId);

        // Assert — one read, the reconcile's own link load, then a fresh read.
        _linkRepository.Received(3).Query();
    }
}
