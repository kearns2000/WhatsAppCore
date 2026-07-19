namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// An inbound reply to a quick-reply button on a template message previously sent by this
/// application.
/// </summary>
public sealed record WhatsAppButtonReplyMessageEvent : WhatsAppMessageEvent
{
    /// <summary>
    /// Gets the display text of the button that was tapped.
    /// </summary>
    public required string ButtonText { get; init; }

    /// <summary>
    /// Gets the payload associated with the button, as originally defined on the template.
    /// </summary>
    public string? ButtonPayload { get; init; }
}
