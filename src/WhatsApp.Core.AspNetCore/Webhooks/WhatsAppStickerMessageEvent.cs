namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// An inbound sticker message.
/// </summary>
public sealed record WhatsAppStickerMessageEvent : WhatsAppMessageEvent
{
    /// <summary>
    /// Gets the sticker media attached to this message.
    /// </summary>
    public required WhatsAppInboundMedia Sticker { get; init; }

    /// <summary>
    /// Gets a value indicating whether this sticker is animated.
    /// </summary>
    public bool IsAnimated { get; init; }
}
