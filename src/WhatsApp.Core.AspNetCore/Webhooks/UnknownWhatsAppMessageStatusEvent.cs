namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// A message status update reporting a status value not (yet) modeled by this library, such as
/// a new status introduced by Meta after this package was published.
/// </summary>
public sealed record UnknownWhatsAppMessageStatusEvent : WhatsAppMessageStatusEvent
{
    /// <summary>
    /// Gets the raw <c>status</c> field reported by Meta, unrecognized by this version of this
    /// library.
    /// </summary>
    public required string Status { get; init; }
}
