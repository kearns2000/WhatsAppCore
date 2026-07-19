using WhatsApp.Core.AspNetCore.Webhooks;

namespace WhatsApp.Core.AspNetCore.Dispatch;

/// <summary>
/// Computes stable, collision-resistant deduplication keys for webhook events, suitable for use
/// as the key in a durable <see cref="IWhatsAppWebhookDeduplicator"/> implementation.
/// </summary>
public static class WhatsAppWebhookDedupKey
{
    /// <summary>
    /// Computes a stable key for <paramref name="notification"/>, derived from the WhatsApp
    /// Business Account id, the concrete event type, and the underlying message or status
    /// message id.
    /// </summary>
    /// <param name="notification">The event to compute a key for.</param>
    /// <returns>
    /// A key of the form <c>{wabaId}:{eventTypeName}:{messageId}</c>. Two events represent the
    /// same underlying occurrence (e.g. the same redelivered message, or the same message
    /// reaching the same status twice) if and only if they produce the same key.
    /// </returns>
    public static string For(WhatsAppWebhookEvent notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var eventType = notification.GetType().Name;
        var messageId = notification switch
        {
            WhatsAppMessageEvent messageEvent => messageEvent.MessageId,
            WhatsAppMessageStatusEvent statusEvent => statusEvent.MessageId,
            _ => Guid.NewGuid().ToString("N"),
        };

        return $"{notification.WhatsAppBusinessAccountId}:{eventType}:{messageId}";
    }
}
