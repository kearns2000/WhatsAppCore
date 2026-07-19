namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// An inbound location share.
/// </summary>
public sealed record WhatsAppLocationMessageEvent : WhatsAppMessageEvent
{
    /// <summary>
    /// Gets the shared location's latitude.
    /// </summary>
    public required double Latitude { get; init; }

    /// <summary>
    /// Gets the shared location's longitude.
    /// </summary>
    public required double Longitude { get; init; }

    /// <summary>
    /// Gets the name of the shared location (e.g. a business or landmark name), if provided.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the street address of the shared location, if provided.
    /// </summary>
    public string? Address { get; init; }
}
