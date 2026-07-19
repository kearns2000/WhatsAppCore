namespace WhatsApp.Core.Messages;

/// <summary>
/// Identifies a previous message that a new message is replying to, causing the WhatsApp client
/// to render the new message as an inline reply/quote.
/// </summary>
public sealed record WhatsAppReplyContext
{
    /// <summary>
    /// Gets the WhatsApp message id (<c>wamid...</c>) of the message being replied to.
    /// </summary>
    public required string MessageId { get; init; }
}
