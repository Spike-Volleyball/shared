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
        /// Where a Tap to Pay reader is bound when the event has no club venue to name —
        /// Spike's own registered address. Not a placeholder for a missing venue lookup:
        /// plenty of events are created with a free-text location, and a Location is still
        /// required to connect a reader at all.
        /// </summary>
        public TerminalFallbackLocation TerminalFallbackLocation { get; set; } = new();
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

    /// <summary>
    /// A Stripe Terminal Location is created from a postal address, and its country is fixed
    /// at creation — so this is configuration rather than something derived at runtime.
    /// </summary>
    public class TerminalFallbackLocation
    {
        public string? DisplayName { get; set; }
        public string? Line1 { get; set; }
        public string? City { get; set; }
        public string? Region { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }

        /// <summary>GB needs line1, city and postcode; every country needs a country.</summary>
        public bool IsUsable =>
            !string.IsNullOrWhiteSpace(Line1)
            && !string.IsNullOrWhiteSpace(City)
            && !string.IsNullOrWhiteSpace(PostalCode)
            && !string.IsNullOrWhiteSpace(Country);
    }
}
