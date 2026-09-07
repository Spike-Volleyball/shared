namespace Shared.Options
{
    public class StripeSettings
    {
        public string? ApiKey { get; set; }
        public string? RefreshUrl { get; set; }
        public string? ReturnUrl { get; set; }
        public string? CheckoutSuccessUrl { get; set; }
        public string? CheckoutCancelUrl { get; set; }

        /// <summary>
        /// The Terminal Location a Tap to Pay reader is bound to at connect time. Under
        /// destination charges Locations belong to the platform, so one per country is
        /// correct today — every club is GB, and within a country a Location only decides
        /// the regional acceptance rules. Per-venue Locations, keyed off the venue address
        /// clubs-service already holds, are the upgrade; see ResolveTerminalLocationAsync.
        /// </summary>
        public string? TerminalLocationId { get; set; }
        public string? WebhookSecret { get; set; }

        // Connected-account subscription events (customer.subscription.*, invoice.*) are delivered
        // to a separate Stripe Connect endpoint, which has its own signing secret. One secret
        // cannot verify both endpoints.
        public string? ConnectWebhookSecret { get; set; }

        // Set to undercut Spond, the incumbent in UK grassroots sport, at every amount: they
        // charge a flat 0.20 PLUS 2.5%, so max(2.5%, 0.20) is cheaper by 2.5% of the payment
        // below 8.00 and by a flat 0.20 above it. These are defaults rather than dead values —
        // nothing in the environment overrides them, so production runs what is written here.
        public decimal PlatformFeePercent { get; set; } = 2.5m;
        public decimal PlatformFeeMin { get; set; } = 0.20m;

        // Subscriptions carry no minimum: Stripe applies application_fee_percent to the
        // recurring invoice, and a flat floor has nowhere to live in that model.
        public decimal SubscriptionFeePercent { get; set; } = 2.5m;
    }
}
