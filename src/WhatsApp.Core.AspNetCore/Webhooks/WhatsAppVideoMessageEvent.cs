namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// An inbound video message.
/// </summary>
public sealed record WhatsAppVideoMessageEvent : WhatsAppMessageEvent
{
    /// <summary>
    /// Gets the video media attached to this message.
    /// </summary>
    public required WhatsAppInboundMedia Video { get; init; }
}
