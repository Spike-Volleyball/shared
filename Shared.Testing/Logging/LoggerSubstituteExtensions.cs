using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Shared.Testing.Logging;

public static class LoggerSubstituteExtensions
{
    /// <summary>Every message an NSubstitute logger was asked to write, rendered as a sink would.</summary>
    public static IReadOnlyList<string> LoggedMessages(this ILogger logger) =>
        logger.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(ILogger.Log))
            .Select(call => call.GetArguments()[2]?.ToString() ?? string.Empty)
            .ToList();
}
