namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// An inbound free-form text message.
/// </summary>
public sealed record WhatsAppTextMessageEvent : WhatsAppMessageEvent
{
    /// <summary>
    /// Gets the text content of the message.
    /// </summary>
    public required string Body { get; init; }
}
