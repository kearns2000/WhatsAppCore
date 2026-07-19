namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// An inbound image message.
/// </summary>
public sealed record WhatsAppImageMessageEvent : WhatsAppMessageEvent
{
    /// <summary>
    /// Gets the image media attached to this message.
    /// </summary>
    public required WhatsAppInboundMedia Image { get; init; }
}
