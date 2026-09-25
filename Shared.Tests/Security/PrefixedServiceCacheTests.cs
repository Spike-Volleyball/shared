using System.Net;
using System.Net.Http.Headers;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Shared.Middleware;
using Shared.Models.Jwt;

namespace Shared.Tests.Security;

/// <summary>
/// A service whose own cache prefixes its keys, against a real Redis that auth has written its
/// revocations to without one.
/// </summary>
[TestFixture]
[Category("Integration")]
public class PrefixedServiceCacheTests
{
    private IContainer _redis = null!;
    private RedisCache _authsCache = null!;
    private SecuredTestApp _app = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _redis = new ContainerBuilder()
            .WithImage("redis:7-alpine")
            .WithPortBinding(6379, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("redis-cli", "ping"))
            .Build();
        await _redis.StartAsync();
        var connection = $"{_redis.Hostname}:{_redis.GetMappedPublicPort(6379)}";

        _authsCache = new RedisCache(Microsoft.Extensions.Options.Options.Create(
            new RedisCacheOptions { Configuration = connection }));

        _app = await SecuredTestApp.StartAsync(
            app =>
            {
                app.UseMiddleware<JwtBlacklistMiddleware>();
                app.MapGet("/signed-in-only", () => "ok");
            },
            services => services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = connection;
                options.InstanceName = "social:";
            }));
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _app.DisposeAsync();
        _authsCache.Dispose();
        await _redis.DisposeAsync();
    }

    [Test]
    public async Task ABlacklistedAccount_IsRefused()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await _authsCache.SetStringAsync($"jwt_blacklist:{userId}", "revoked");

        // Act
        var response = await SignedInOnlyAsync(userId);

        // Assert — refused, though the service's own cache looks under its prefix and finds nothing
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ServicesOwnCache().GetStringAsync($"jwt_blacklist:{userId}")).Should().BeNull();
    }

    [Test]
    public async Task AStaleToken_IsRefused()
    {
        // Arrange — the token carries no version, so counts as the first
        var userId = Guid.NewGuid();
        await _authsCache.SetStringAsync(
            SessionVersions.CacheKey(userId.ToString()), (SessionVersions.Initial + 1).ToString());

        // Act
        var response = await SignedInOnlyAsync(userId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ServicesOwnCache().GetStringAsync(SessionVersions.CacheKey(userId.ToString()))).Should().BeNull();
    }

    [Test]
    public async Task ACurrentToken_IsAdmitted()
    {
        // Act
        var response = await SignedInOnlyAsync(Guid.NewGuid());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private IDistributedCache ServicesOwnCache() => _app.Services.GetRequiredService<IDistributedCache>();

    private async Task<HttpResponseMessage> SignedInOnlyAsync(Guid userId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/signed-in-only");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", SecuredTestApp.UserToken(userId));
        return await _app.Client.SendAsync(request);
    }
}
