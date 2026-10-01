using System.Collections.Concurrent;
using System.Text.Json;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace Shared.Tests.Logging;

/// <summary>What the SDK would have sent to Sentry, kept instead of sent.</summary>
internal sealed class RecordingSentryTransport : ITransport
{
    private const string EventItemType = "event";

    private readonly ConcurrentQueue<string?> _itemTypes = new();
    private readonly ConcurrentQueue<JsonElement> _events = new();

    public IReadOnlyCollection<string?> ItemTypes => _itemTypes;

    /// <summary>Each error event, as the JSON Sentry would have received.</summary>
    public IReadOnlyCollection<JsonElement> Events => _events;

    public async Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        foreach (var item in envelope.Items)
        {
            var type = item.TryGetType();
            _itemTypes.Enqueue(type);

            if (type != EventItemType)
                continue;

            using var payload = new MemoryStream();
            await item.Payload.SerializeAsync(payload, null, cancellationToken);
            _events.Enqueue(JsonDocument.Parse(payload.ToArray()).RootElement.Clone());
        }
    }
}
