using WhatsApp.Core.AspNetCore.Webhooks;

namespace WhatsApp.Core.AspNetCore.Dispatch;

/// <summary>
/// An <see cref="IWhatsAppWebhookDeduplicator"/> that accepts every event without tracking
/// state. Prefer the default <see cref="MemoryWhatsAppWebhookDeduplicator"/> (or a durable
/// storage-backed implementation) unless handlers are fully idempotent. Register this type
/// explicitly when you intentionally want no deduplication.
/// </summary>
public sealed class NoOpWhatsAppWebhookDeduplicator : IWhatsAppWebhookDeduplicator
{
    /// <inheritdoc />
    public Task<bool> TryAcceptAsync(WhatsAppWebhookEvent notification, CancellationToken stopToken) =>
        Task.FromResult(true);
}
