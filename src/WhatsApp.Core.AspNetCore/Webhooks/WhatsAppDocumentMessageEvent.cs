namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// An inbound document (file) message.
/// </summary>
public sealed record WhatsAppDocumentMessageEvent : WhatsAppMessageEvent
{
    /// <summary>
    /// Gets the document media attached to this message.
    /// </summary>
    public required WhatsAppInboundMedia Document { get; init; }
}
