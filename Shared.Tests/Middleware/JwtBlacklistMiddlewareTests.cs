using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Middleware;
using Shared.Models.Jwt;
using Shared.Options;
using Shared.Testing.Base;

namespace Shared.Tests.Middleware;

[TestFixture]
[Category("Unit")]
public class JwtBlacklistMiddlewareTests : UnitTestBase
{
    private const int CacheWindowSeconds = 30;

    private static readonly string UserId = Guid.NewGuid().ToString();
    private static readonly string BlacklistKey = $"jwt_blacklist:{UserId}";
    private static readonly string SessionVersionKey = SessionVersions.CacheKey(UserId);

    private IDistributedCache _cache = null!;
    private bool _nextRan;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _cache = Substitute.For<IDistributedCache>();
        _nextRan = false;

        // Unconfigured, the substitute answers an empty array rather than the null of a missing key.
        Store(BlacklistKey, null);
        Store(SessionVersionKey, null);
    }

    private JwtBlacklistMiddleware Sut(int cacheWindowSeconds = CacheWindowSeconds) => new(
        _ =>
        {
            _nextRan = true;
            return Task.CompletedTask;
        },
        Microsoft.Extensions.Options.Options.Create(new JwtSettings { SessionVersionCacheSeconds = cacheWindowSeconds }),
        TimeProvider);

    private void Store(string key, string? value) =>
        _cache.GetAsync(key, Arg.Any<CancellationToken>())
            .Returns(value is null ? null : Encoding.UTF8.GetBytes(value));

    private static DefaultHttpContext SignedIn(int? sessionVersion, string? userId = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId ?? UserId) };
        if (sessionVersion is { } version)
            claims.Add(new Claim(SessionVersions.ClaimType, version.ToString()));

        return new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) };
    }

    private Task SessionVersionReads(int expected) =>
        _cache.Received(expected).GetAsync(SessionVersionKey, Arg.Any<CancellationToken>());

    [Test]
    public async Task SignedOut_PassesWithoutReadingRedis()
    {
        // Act
        await Sut().InvokeAsync(new DefaultHttpContext(), _cache);

        // Assert
        _nextRan.Should().BeTrue();
        await _cache.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task BlacklistedAccount_IsRefusedWithoutReadingTheVersion()
    {
        // Arrange
        Store(BlacklistKey, "revoked");

        // Act
        var act = () => Sut().InvokeAsync(SignedIn(sessionVersion: 1), _cache);

        // Assert
        (await act.Should().ThrowAsync<UnauthorizedException>())
            .Which.ErrorCode.Should().Be(ErrorCodeEnum.TokenInvalid);
        _nextRan.Should().BeFalse();
        await SessionVersionReads(0);
    }

    [Test]
    public async Task TokenOlderThanTheRecordedVersion_IsRefusedAsARevokedToken()
    {
        // Arrange — the password was replaced after this token was minted
        Store(SessionVersionKey, "4");

        // Act
        var act = () => Sut().InvokeAsync(SignedIn(sessionVersion: 3), _cache);

        // Assert
        (await act.Should().ThrowAsync<UnauthorizedException>())
            .Which.ErrorCode.Should().Be(ErrorCodeEnum.TokenInvalid);
        _nextRan.Should().BeFalse();
    }

    [Test]
    public async Task TokenAtTheRecordedVersion_PassesThrough()
    {
        // Arrange — minted by the sign-in that followed the move
        Store(SessionVersionKey, "4");

        // Act
        await Sut().InvokeAsync(SignedIn(sessionVersion: 4), _cache);

        // Assert
        _nextRan.Should().BeTrue();
    }

    [Test]
    public async Task TokenNewerThanTheRecordedVersion_PassesThrough()
    {
        // Arrange — a later move whose record never reached Redis
        Store(SessionVersionKey, "4");

        // Act
        await Sut().InvokeAsync(SignedIn(sessionVersion: 5), _cache);

        // Assert
        _nextRan.Should().BeTrue();
    }

    [Test]
    public async Task TokenWithoutAVersion_CountsAsTheFirst()
    {
        // Arrange
        Store(SessionVersionKey, "2");

        // Act
        var act = () => Sut().InvokeAsync(SignedIn(sessionVersion: null), _cache);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Test]
    public async Task TokenWithoutAVersion_PassesWhileTheAccountIsAtTheFirst()
    {
        // Arrange
        Store(SessionVersionKey, SessionVersions.Initial.ToString());

        // Act
        await Sut().InvokeAsync(SignedIn(sessionVersion: null), _cache);

        // Assert
        _nextRan.Should().BeTrue();
    }

    [Test]
    public async Task NoRecordedVersion_PassesThrough()
    {
        // Act — nothing moved lately: the record outlives every token it could refuse
        await Sut().InvokeAsync(SignedIn(sessionVersion: 1), _cache);

        // Assert
        _nextRan.Should().BeTrue();
    }

    [Test]
    public async Task WithinTheWindow_TheRecordIsReadOnce()
    {
        // Arrange
        Store(SessionVersionKey, "4");
        var sut = Sut();
        await sut.InvokeAsync(SignedIn(sessionVersion: 4), _cache);

        // Act
        AdvanceTime(TimeSpan.FromSeconds(CacheWindowSeconds - 1));
        await sut.InvokeAsync(SignedIn(sessionVersion: 4), _cache);

        // Assert
        await SessionVersionReads(1);
    }

    [Test]
    public async Task AMoveRecordedWithinTheWindow_IsRefusedOnceTheWindowHasPassed()
    {
        // Arrange — this instance last looked before the move, and found no record
        var sut = Sut();
        await sut.InvokeAsync(SignedIn(sessionVersion: 1), _cache);
        Store(SessionVersionKey, "2");

        // Act
        AdvanceTime(TimeSpan.FromSeconds(CacheWindowSeconds - 1));
        await sut.InvokeAsync(SignedIn(sessionVersion: 1), _cache);
        AdvanceTime(TimeSpan.FromSeconds(1));
        var act = () => sut.InvokeAsync(SignedIn(sessionVersion: 1), _cache);

        // Assert — the stale "no record" held for the window and no longer
        await act.Should().ThrowAsync<UnauthorizedException>();
        await SessionVersionReads(2);
    }

    [Test]
    public async Task TheWindowIsPerAccount()
    {
        // Arrange — one account's answer is in memory, another's has moved on
        var otherUserId = Guid.NewGuid().ToString();
        Store($"jwt_blacklist:{otherUserId}", null);
        Store(SessionVersions.CacheKey(otherUserId), "2");
        var sut = Sut();
        await sut.InvokeAsync(SignedIn(sessionVersion: 1), _cache);

        // Act
        var act = () => sut.InvokeAsync(SignedIn(sessionVersion: 1, otherUserId), _cache);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Test]
    public async Task WithNoWindow_EveryRequestReadsTheRecord()
    {
        // Arrange
        var sut = Sut(cacheWindowSeconds: 0);
        await sut.InvokeAsync(SignedIn(sessionVersion: 1), _cache);
        Store(SessionVersionKey, "2");

        // Act
        var act = () => sut.InvokeAsync(SignedIn(sessionVersion: 1), _cache);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [TestCase("blacklist")]
    [TestCase("session version")]
    public async Task WhenARedisReadFails_TheRequestFailsWithIt(string read)
    {
        // Arrange — the version read fails exactly as the blacklist read already does
        var failure = new TimeoutException("Redis did not answer");
        _cache.GetAsync(read == "blacklist" ? BlacklistKey : SessionVersionKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<byte[]?>(failure));

        // Act
        var act = () => Sut().InvokeAsync(SignedIn(sessionVersion: 1), _cache);

        // Assert
        (await act.Should().ThrowAsync<TimeoutException>()).Which.Should().BeSameAs(failure);
        _nextRan.Should().BeFalse();
    }

    [Test]
    public async Task AFailedRead_IsNotRememberedAsNoRecord()
    {
        // Arrange
        _cache.GetAsync(SessionVersionKey, Arg.Any<CancellationToken>()).Returns(
            Task.FromException<byte[]?>(new TimeoutException("Redis did not answer")),
            Task.FromResult<byte[]?>(Encoding.UTF8.GetBytes("2")));
        var sut = Sut();
        await FluentActions.Awaiting(() => sut.InvokeAsync(SignedIn(sessionVersion: 1), _cache))
            .Should().ThrowAsync<TimeoutException>();

        // Act
        var act = () => sut.InvokeAsync(SignedIn(sessionVersion: 1), _cache);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
