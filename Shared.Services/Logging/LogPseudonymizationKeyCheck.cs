using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Options;

namespace Shared.Services.Logging;

/// <summary>
/// Says at startup, once, that email addresses and phone numbers in this service's logs cannot
/// be correlated.
/// A warning and never a failure: the masks alone are safe, and a missing secret must not take
/// a service down.
/// </summary>
public sealed class LogPseudonymizationKeyCheck(
    IOptions<LogPseudonymizationSettings> options,
    ILogger<LogPseudonymizationKeyCheck> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.HasUsableKey)
        {
            logger.LogWarning(
                "{Setting} is missing or shorter than {MinimumLength} characters: email addresses and phone numbers are logged as masks only and cannot be correlated",
                $"{LogPseudonymizationSettings.SectionName}:{nameof(LogPseudonymizationSettings.HmacKey)}",
                LogPseudonymizationSettings.MinimumKeyLength);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
