namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// A single contact card shared by a user via a WhatsApp "contact" message.
/// </summary>
public sealed record WhatsAppInboundContactCard
{
    /// <summary>
    /// Gets the contact's formatted display name, if reported.
    /// </summary>
    public string? FormattedName { get; init; }

    /// <summary>
    /// Gets the phone numbers included on this contact card.
    /// </summary>
    public IReadOnlyList<WhatsAppInboundContactPhone> Phones { get; init; } = [];
}

/// <summary>
/// A single phone number entry on a <see cref="WhatsAppInboundContactCard"/>.
/// </summary>
public sealed record WhatsAppInboundContactPhone
{
    /// <summary>
    /// Gets the phone number as formatted by the sender's client.
    /// </summary>
    public string? Phone { get; init; }

    /// <summary>
    /// Gets the WhatsApp id associated with this phone number, if it belongs to a WhatsApp
    /// user.
    /// </summary>
    public string? WaId { get; init; }

    /// <summary>
    /// Gets the label associated with this phone number (e.g. <c>"CELL"</c>, <c>"HOME"</c>,
    /// <c>"WORK"</c>), if reported.
    /// </summary>
    public string? Type { get; init; }
}
