namespace CarshiTow.Application.Configuration;

/// <summary>Stripe Payment Intents + webhooks (SRS §2.3, §7.5, §7.8, §12).</summary>
public sealed class StripeSettings
{
    public const string SectionName = "StripePayments";

    /// <summary>Secret API key (sk_live_… / sk_test_…).</summary>
    public string SecretKey { get; set; } = "";

    /// <summary>Webhook signing secret (whsec_…).</summary>
    public string WebhookSecret { get; set; } = "";

    /// <summary>Optional publishable key for frontends (pk_…).</summary>
    public string? PublishableKey { get; set; }
}
