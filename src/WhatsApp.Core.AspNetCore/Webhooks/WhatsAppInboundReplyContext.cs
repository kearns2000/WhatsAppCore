namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// Identifies the earlier message that an inbound message is replying to, when the sender used
/// their WhatsApp client's reply/quote feature.
/// </summary>
public sealed record WhatsAppInboundReplyContext
{
    /// <summary>
    /// Gets the WhatsApp message id (<c>wamid...</c>) of the message being replied to.
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    /// Gets the WhatsApp id of the participant who sent the original message being replied to,
    /// if reported.
    /// </summary>
    public string? From { get; init; }
}
