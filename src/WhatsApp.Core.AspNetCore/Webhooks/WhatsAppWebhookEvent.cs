using System.Text.Json;

namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// The common base of every strongly-typed event produced by parsing an inbound WhatsApp webhook
/// delivery. A single webhook delivery typically expands into zero or more of these events (one
/// per message or status update).
/// </summary>
public abstract record WhatsAppWebhookEvent
{
    /// <summary>
    /// Gets the id of the WhatsApp Business Account that this event was reported against (the
    /// <c>entry[].id</c> field of the webhook payload).
    /// </summary>
    public required string WhatsAppBusinessAccountId { get; init; }

    /// <summary>
    /// Gets the Meta-assigned phone number id the event is addressed to
    /// (<c>entry[].changes[].value.metadata.phone_number_id</c>), if present.
    /// </summary>
    public string? PhoneNumberId { get; init; }

    /// <summary>
    /// Gets the human-readable display phone number the event is addressed to
    /// (<c>entry[].changes[].value.metadata.display_phone_number</c>), if present.
    /// </summary>
    public string? DisplayPhoneNumber { get; init; }

    /// <summary>
    /// Gets the UTC instant at which the enclosing webhook delivery was received by this
    /// application, as measured by the server's <see cref="TimeProvider"/>. Distinct from any
    /// message- or status-specific timestamp carried on the payload itself.
    /// </summary>
    public required DateTimeOffset ReceivedAt { get; init; }

    /// <summary>
    /// Gets the raw JSON element this event was parsed from (the individual message or status
    /// object, not the entire webhook payload), for forward-compatible access to fields not yet
    /// modeled by this library.
    /// </summary>
    public JsonElement? ExtensionData { get; init; }
}
