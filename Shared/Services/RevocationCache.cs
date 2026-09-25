using Microsoft.Extensions.Caching.Distributed;

namespace Shared.Services;

/// <summary>
/// The cache auth's revocations live in: the blacklist and each account's session version, which
/// auth writes and every service's JwtBlacklistMiddleware reads. Its keys carry no service's
/// prefix, so a service whose own cache puts one before every key ("social:") still finds them.
/// </summary>
/// <remarks>
/// A type of its own rather than a second <see cref="IDistributedCache" /> registration: test
/// hosts swap the service's cache for an in-memory one by removing every registration of that
/// type, and this one must survive to read whichever cache takes its place.
/// </remarks>
public sealed class RevocationCache(IDistributedCache cache)
{
    public IDistributedCache Cache { get; } = cache;
}
