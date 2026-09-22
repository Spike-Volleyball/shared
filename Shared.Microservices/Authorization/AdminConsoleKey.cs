using System.Security.Cryptography;
using System.Text;

namespace Shared.Microservices.Authorization;

/// <summary>
/// The one comparison of a presented admin console key against the configured secret.
/// </summary>
/// <remarks>
/// Shared by the filter and the authentication scheme on purpose. A second copy of a
/// constant-time comparison is how one of them quietly becomes a byte-by-byte one.
/// </remarks>
public static class AdminConsoleKey
{
    public const string HeaderName = "X-Admin-Console-Key";

    public static bool Matches(AdminConsoleSettings settings, string presented)
    {
        /*
         * Fails closed when unconfigured. A deployment that forgets the secret must refuse
         * every request rather than accept an empty key, which is what a naive equality
         * check against an unset setting would do.
         */
        if (string.IsNullOrEmpty(settings.ApiKey))
            return false;

        /*
         * Compared as hashes rather than as the raw strings. FixedTimeEquals returns false
         * immediately when the lengths differ, so comparing the keys directly would leak the
         * key's length; digests are always the same width. The fixed-time comparison itself
         * matters because a byte-by-byte one leaks the prefix to anyone able to time
         * responses, and the gateway makes these endpoints reachable from the internet.
         */
        var expected = SHA256.HashData(Encoding.UTF8.GetBytes(settings.ApiKey));
        var actual = SHA256.HashData(Encoding.UTF8.GetBytes(presented));

        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
