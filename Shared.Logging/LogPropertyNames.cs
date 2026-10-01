namespace Shared.Logging;

/// <summary>
/// Names of log properties that the code pushing them and the logging pipeline have to agree on.
/// </summary>
public static class LogPropertyNames
{
    /// <summary>The address a request came from. Written to the log; never handed to Sentry.</summary>
    public const string ClientIp = "ClientIp";
}
