namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// The common base of every event that reports a delivery status update for a message this
/// application previously sent (as opposed to an inbound message sent by a WhatsApp user).
/// </summary>
public abstract record WhatsAppMessageStatusEvent : WhatsAppWebhookEvent
{
    /// <summary>
    /// Gets the WhatsApp message id (<c>wamid...</c>) of the message this status update
    /// describes. Stable and safe to use as an idempotency key together with the concrete
    /// event type (e.g. sent vs. delivered).
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    /// Gets the WhatsApp id of the message's recipient.
    /// </summary>
    public required string RecipientId { get; init; }

    /// <summary>
    /// Gets the UTC instant at which this status transition occurred, as reported by Meta.
    /// Always converted from the Unix timestamp Meta reports; never adjusted to local time.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }
}
