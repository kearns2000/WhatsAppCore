namespace WhatsApp.Core.AspNetCore.Options;

/// <summary>
/// Controls how a batch of <see cref="Webhooks.WhatsAppWebhookEvent"/> instances parsed from a
/// single webhook delivery is dispatched to registered handlers.
/// </summary>
public enum WhatsAppWebhookDispatchMode
{
    /// <summary>
    /// Dispatch events one at a time, in the order they appeared in the webhook payload,
    /// awaiting each before starting the next. Guarantees in-order processing at the cost of
    /// throughput; the safe default.
    /// </summary>
    Sequential = 0,

    /// <summary>
    /// Dispatch events concurrently, bounded by
    /// <see cref="WhatsAppWebhookOptions.MaxDegreeOfParallelism"/>. Improves throughput for
    /// webhooks that batch many events, but does not preserve processing order across events.
    /// </summary>
    Parallel = 1,
}
