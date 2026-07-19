namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// The common base of every event that represents an inbound message sent by a WhatsApp user
/// (as opposed to a delivery status update for a message this application sent).
/// </summary>
public abstract record WhatsAppMessageEvent : WhatsAppWebhookEvent
{
    /// <summary>
    /// Gets the WhatsApp message id (<c>wamid...</c>) of this message. Stable and safe to use
    /// as an idempotency key.
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    /// Gets the WhatsApp id (typically a phone number in international format, without a
    /// leading <c>+</c>) of the user who sent this message.
    /// </summary>
    public required string From { get; init; }

    /// <summary>
    /// Gets the UTC instant at which the sender's WhatsApp client sent this message, as
    /// reported by Meta. Always converted from the Unix timestamp Meta reports; never adjusted
    /// to local time.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets the message this message is a reply to, if the sender used their client's
    /// reply/quote feature.
    /// </summary>
    public WhatsAppInboundReplyContext? Context { get; init; }
}
