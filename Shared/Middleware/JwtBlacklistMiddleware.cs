using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Models.Jwt;
using Shared.Options;

namespace Shared.Middleware;

public class JwtBlacklistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _sessionVersionCacheWindow;
    private readonly MemoryCache _recordedVersions;

    /// <remarks>
    /// The settings and the clock are optional because services' own tests build this middleware
    /// around its next delegate alone.
    /// </remarks>
    public JwtBlacklistMiddleware(
        RequestDelegate next,
        IOptions<JwtSettings>? jwtSettings = null,
        TimeProvider? timeProvider = null)
    {
        _next = next;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _sessionVersionCacheWindow = TimeSpan.FromSeconds(
            (jwtSettings?.Value ?? new JwtSettings()).SessionVersionCacheSeconds);
        _recordedVersions = new MemoryCache(new MemoryCacheOptions { Clock = new TimeProviderClock(_timeProvider) });
    }

    public async Task InvokeAsync(HttpContext context, IDistributedCache cache)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // Check if all tokens for this user have been blacklisted (e.g., consent revocation)
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var blacklisted = await cache.GetStringAsync($"jwt_blacklist:{userId}");
                if (blacklisted != null || await HasSessionEndedAsync(context.User, userId, cache))
                {
                    throw new UnauthorizedException(
                        "Token has been revoked",
                        ErrorCodeEnum.TokenInvalid);
                }
            }
        }

        await _next(context);
    }

    /// <summary>
    /// Whether the token was minted before auth last moved its account's session version on: the
    /// password was replaced, say, so every session the account had is over, this one included.
    /// </summary>
    private async Task<bool> HasSessionEndedAsync(ClaimsPrincipal user, string userId, IDistributedCache cache) =>
        await RecordedSessionVersionAsync(userId, cache) > TokenSessionVersion(user);

    /// <summary>
    /// What auth last recorded for the account, or null when it recorded nothing, remembered for the
    /// window either way. A failed read is not remembered: it fails the request, as a failed
    /// blacklist read does, and the next request asks again.
    /// </summary>
    private Task<int?> RecordedSessionVersionAsync(string userId, IDistributedCache cache) =>
        _recordedVersions.GetOrCreateAsync(userId, async entry =>
        {
            entry.AbsoluteExpiration = _timeProvider.GetUtcNow() + _sessionVersionCacheWindow;
            return int.TryParse(await cache.GetStringAsync(SessionVersions.CacheKey(userId)), out var version)
                ? version
                : (int?)null;
        });

    private static int TokenSessionVersion(ClaimsPrincipal user) =>
        int.TryParse(user.FindFirst(SessionVersions.ClaimType)?.Value, out var version)
            ? version
            : SessionVersions.Initial;

    private sealed class TimeProviderClock(TimeProvider timeProvider) : ISystemClock
    {
        public DateTimeOffset UtcNow => timeProvider.GetUtcNow();
    }
}
