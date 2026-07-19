namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// An inbound message sharing one or more contact cards.
/// </summary>
public sealed record WhatsAppContactMessageEvent : WhatsAppMessageEvent
{
    /// <summary>
    /// Gets the shared contact cards. Always contains at least one entry.
    /// </summary>
    public required IReadOnlyList<WhatsAppInboundContactCard> Contacts { get; init; }
}
