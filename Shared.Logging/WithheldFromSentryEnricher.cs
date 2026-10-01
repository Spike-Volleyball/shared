using Serilog.Core;
using Serilog.Events;

namespace Shared.Logging;

/// <summary>
/// Takes out of a log event what Sentry is not to be sent. It runs in the Sentry sink's own
/// sub-logger, which Serilog hands a copy of each event, so the log keeps what this removes.
/// </summary>
internal sealed class WithheldFromSentryEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) =>
        logEvent.RemovePropertyIfPresent(LogPropertyNames.ClientIp);
}
