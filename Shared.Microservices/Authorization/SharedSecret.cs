using System.Security.Cryptography;
using System.Text;

namespace Shared.Microservices.Authorization;

/// <summary>
/// The one comparison of a secret a caller presents against the configured one: the admin console's key
/// and the web renderer's secret both go through it.
/// </summary>
/// <remarks>
/// One copy on purpose. A second copy of a constant-time comparison is how one of them quietly becomes
/// a byte-by-byte one.
/// </remarks>
public static class SharedSecret
{
    public static bool Matches(string? configured, string? presented)
    {
        /*
         * Fails closed when unconfigured. A deployment that forgets the secret must refuse
         * every request rather than accept an empty one, which is what a naive equality
         * check against an unset setting would do.
         */
        if (string.IsNullOrEmpty(configured))
            return false;

        /*
         * Compared as hashes rather than as the raw strings. FixedTimeEquals returns false
         * immediately when the lengths differ, so comparing the secrets directly would leak the
         * secret's length; digests are always the same width. The fixed-time comparison itself
         * matters because a byte-by-byte one leaks the prefix to anyone able to time
         * responses, and the gateway makes these endpoints reachable from the internet.
         */
        var expected = SHA256.HashData(Encoding.UTF8.GetBytes(configured));
        var actual = SHA256.HashData(Encoding.UTF8.GetBytes(presented ?? string.Empty));

        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
