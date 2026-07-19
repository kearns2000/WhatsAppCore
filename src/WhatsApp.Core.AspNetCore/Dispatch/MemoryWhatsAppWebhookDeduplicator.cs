using System.Collections.Concurrent;
using WhatsApp.Core.AspNetCore.Webhooks;

namespace WhatsApp.Core.AspNetCore.Dispatch;

/// <summary>
/// An in-process <see cref="IWhatsAppWebhookDeduplicator"/> that rejects events whose
/// <see cref="WhatsAppWebhookDedupKey"/> was seen within a sliding retention window. This is the
/// default registered by <c>AddWhatsAppWebhooks</c>; it mitigates simple replay on a single
/// process but is not a substitute for durable, shared-store deduplication across instances.
/// </summary>
public sealed class MemoryWhatsAppWebhookDeduplicator : IWhatsAppWebhookDeduplicator
{
    /// <summary>
    /// The default retention window for remembered dedup keys (24 hours).
    /// </summary>
    public static readonly TimeSpan DefaultRetention = TimeSpan.FromHours(24);

    private readonly ConcurrentDictionary<string, long> _seen = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _retention;
    private long _lastPruneTicks;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryWhatsAppWebhookDeduplicator"/> class
    /// with <see cref="DefaultRetention"/>.
    /// </summary>
    /// <param name="timeProvider">Supplies the current UTC time for expiry.</param>
    public MemoryWhatsAppWebhookDeduplicator(TimeProvider timeProvider)
        : this(timeProvider, DefaultRetention)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryWhatsAppWebhookDeduplicator"/> class.
    /// </summary>
    /// <param name="timeProvider">Supplies the current UTC time for expiry.</param>
    /// <param name="retention">How long accepted keys are remembered before they may be accepted again.</param>
    public MemoryWhatsAppWebhookDeduplicator(TimeProvider timeProvider, TimeSpan retention)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (retention <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retention), retention, "Retention must be a positive duration.");
        }

        _timeProvider = timeProvider;
        _retention = retention;
    }

    /// <inheritdoc />
    public Task<bool> TryAcceptAsync(WhatsAppWebhookEvent notification, CancellationToken stopToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        stopToken.ThrowIfCancellationRequested();

        PruneExpiredEntriesIfDue();

        var key = WhatsAppWebhookDedupKey.For(notification);
        var nowTicks = _timeProvider.GetUtcNow().UtcTicks;
        var expiresAtTicks = _timeProvider.GetUtcNow().Add(_retention).UtcTicks;

        // Retry briefly so an expired entry can be replaced without a permanent false reject.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (_seen.TryAdd(key, expiresAtTicks))
            {
                return Task.FromResult(true);
            }

            if (!_seen.TryGetValue(key, out var existingExpiresAt) || existingExpiresAt > nowTicks)
            {
                return Task.FromResult(false);
            }

            if (_seen.TryUpdate(key, expiresAtTicks, existingExpiresAt))
            {
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    private void PruneExpiredEntriesIfDue()
    {
        var nowTicks = _timeProvider.GetUtcNow().UtcTicks;
        // Avoid scanning on every event; prune at most once per minute.
        if (nowTicks - Interlocked.Read(ref _lastPruneTicks) < TimeSpan.FromMinutes(1).Ticks)
        {
            return;
        }

        Interlocked.Exchange(ref _lastPruneTicks, nowTicks);

        foreach (var pair in _seen)
        {
            if (pair.Value <= nowTicks)
            {
                _seen.TryRemove(pair.Key, out _);
            }
        }
    }
}
