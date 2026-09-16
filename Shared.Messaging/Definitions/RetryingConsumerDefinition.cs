using MassTransit;

namespace Shared.Messaging.Definitions;

/// <summary>
/// The retry policy for a consumer whose work can run twice without harm. MassTransit
/// retries nothing on its own: without a policy, a transient failure faults the message
/// to the _error queue on its first and only attempt.
///
/// Five attempts over roughly half a minute: enough to ride out a restart on the far end
/// of a gRPC call or a database failover, short enough not to hold a consumer slot for
/// minutes. What outlasts it still lands in the _error queue, which is inspectable.
///
/// Only for idempotent consumers. A retry re-runs the whole consumer, so one that sends an
/// email, a push or a payment before it fails sends it again.
/// </summary>
public class RetryingConsumerDefinition<TConsumer> : ConsumerDefinition<TConsumer>
    where TConsumer : class, IConsumer
{
    private const int RetryLimit = 5;
    private static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan IntervalDelta = TimeSpan.FromSeconds(3);

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<TConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Exponential(RetryLimit, MinInterval, MaxInterval, IntervalDelta));
    }
}

/// <remarks>
/// Deliberately not in Shared.Messaging.Extensions: messages-service declares its own
/// AddRetryingConsumer and imports that namespace, so the same name there would make its calls
/// ambiguous.
/// </remarks>
public static class RetryingConsumerRegistration
{
    public static void AddRetryingConsumer<TConsumer>(this IBusRegistrationConfigurator bus)
        where TConsumer : class, IConsumer =>
        bus.AddConsumer<TConsumer, RetryingConsumerDefinition<TConsumer>>();
}
