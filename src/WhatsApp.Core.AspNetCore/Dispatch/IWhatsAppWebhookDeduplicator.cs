using WhatsApp.Core.AspNetCore.Webhooks;

namespace WhatsApp.Core.AspNetCore.Dispatch;

/// <summary>
/// Decides whether a webhook event should be processed or discarded as a duplicate. Meta may
/// redeliver the same webhook more than once (e.g. after a slow or failed response), so
/// handlers that are not naturally idempotent should be paired with a deduplicator backed by
/// durable storage (e.g. a database or distributed cache) keyed by
/// <see cref="WhatsAppWebhookDedupKey"/>.
/// </summary>
public interface IWhatsAppWebhookDeduplicator
{
    /// <summary>
    /// Determines whether <paramref name="notification"/> should be accepted for processing.
    /// </summary>
    /// <param name="notification">The event to check.</param>
    /// <param name="stopToken">A token that is canceled if the underlying HTTP request is aborted.</param>
    /// <returns>
    /// <see langword="true"/> if this is the first time this event has been seen and it should
    /// be dispatched to handlers; <see langword="false"/> if it is a duplicate and should be
    /// silently skipped.
    /// </returns>
    Task<bool> TryAcceptAsync(WhatsAppWebhookEvent notification, CancellationToken stopToken);
}
