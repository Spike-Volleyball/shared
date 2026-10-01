namespace Shared.Logging;

/// <summary>
/// Names of the loggers the logging pipeline treats differently from the rest.
/// </summary>
internal static class LogSourceNames
{
    /// <summary>EF's logger for the commands it sends: every statement, and each one that fails.</summary>
    public const string DatabaseCommand = "Microsoft.EntityFrameworkCore.Database.Command";
}
