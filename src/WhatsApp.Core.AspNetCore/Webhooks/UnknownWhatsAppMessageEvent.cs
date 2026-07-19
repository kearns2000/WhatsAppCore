using System.Text.Json;

namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// An inbound message of a type not (yet) modeled by this library, such as a new message type
/// introduced by Meta after this package was published. Carries the raw, unmodified JSON so
/// that callers can still handle it.
/// </summary>
public sealed record UnknownWhatsAppMessageEvent : WhatsAppMessageEvent
{
    /// <summary>
    /// Gets the raw <c>type</c> field reported by Meta for this message (e.g. <c>"order"</c>,
    /// <c>"system"</c>), unrecognized by this version of this library.
    /// </summary>
    public required string MessageType { get; init; }

    /// <summary>
    /// Gets the complete, raw JSON object for this message, exactly as received.
    /// </summary>
    public required JsonElement RawContent { get; init; }
}
