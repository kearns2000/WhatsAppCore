namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// An inbound emoji reaction to a previous message.
/// </summary>
public sealed record WhatsAppReactionMessageEvent : WhatsAppMessageEvent
{
    /// <summary>
    /// Gets the WhatsApp message id (<c>wamid...</c>) of the message that was reacted to.
    /// </summary>
    public required string ReactedToMessageId { get; init; }

    /// <summary>
    /// Gets the emoji used for the reaction, or <see langword="null"/> if the sender removed
    /// their reaction.
    /// </summary>
    public string? Emoji { get; init; }
}
