using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using WhatsApp.Core.AspNetCore.Diagnostics;
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
    private readonly ILogger<MemoryWhatsAppWebhookDeduplicator>? _logger;
    private long _lastPruneTicks;
    private int _warned;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryWhatsAppWebhookDeduplicator"/> class
    /// with <see cref="DefaultRetention"/>.
    /// </summary>
    /// <param name="timeProvider">Supplies the current UTC time for expiry.</param>
    public MemoryWhatsAppWebhookDeduplicator(TimeProvider timeProvider)
        : this(timeProvider, DefaultRetention, logger: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryWhatsAppWebhookDeduplicator"/> class.
    /// </summary>
    /// <param name="timeProvider">Supplies the current UTC time for expiry.</param>
    /// <param name="retention">How long accepted keys are remembered before they may be accepted again.</param>
    public MemoryWhatsAppWebhookDeduplicator(TimeProvider timeProvider, TimeSpan retention)
        : this(timeProvider, retention, logger: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryWhatsAppWebhookDeduplicator"/> class.
    /// </summary>
    /// <param name="timeProvider">Supplies the current UTC time for expiry.</param>
    /// <param name="retention">How long accepted keys are remembered before they may be accepted again.</param>
    /// <param name="logger">Optional logger used to warn that in-process dedup is not multi-instance safe.</param>
    public MemoryWhatsAppWebhookDeduplicator(
        TimeProvider timeProvider,
        TimeSpan retention,
        ILogger<MemoryWhatsAppWebhookDeduplicator>? logger)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (retention <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retention), retention, "Retention must be a positive duration.");
        }

        _timeProvider = timeProvider;
        _retention = retention;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<bool> TryAcceptAsync(WhatsAppWebhookEvent notification, CancellationToken stopToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        stopToken.ThrowIfCancellationRequested();

        WarnOnceAboutProcessLocalScope();
        PruneExpiredEntriesIfDue();

        var key = WhatsAppWebhookDedupKey.For(notification);
        var nowTicks = _timeProvider.GetUtcNow().UtcTicks;
        var expiresAtTicks = _timeProvider.GetUtcNow().Add(_retention).UtcTicks;

        // Retry briefly so an expired or concurrently pruned entry can be accepted.
        for (var attempt = 0; attempt < 4; attempt++)
        {
            if (_seen.TryAdd(key, expiresAtTicks))
            {
                return Task.FromResult(true);
            }

            if (!_seen.TryGetValue(key, out var existingExpiresAt))
            {
                // Pruned (or removed) between TryAdd and TryGetValue - retry TryAdd.
                continue;
            }

            if (existingExpiresAt > nowTicks)
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

    private void WarnOnceAboutProcessLocalScope()
    {
        if (_logger is null || Interlocked.Exchange(ref _warned, 1) == 1)
        {
            return;
        }

        WhatsAppWebhookLog.MemoryDeduplicatorProcessLocal(_logger);
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
