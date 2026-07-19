using System.Text.Json.Serialization;

namespace WhatsApp.Core.AspNetCore.Serialization.Wire;

/// <summary>
/// The WhatsApp user profile reported alongside a message, at <c>value.contacts[]</c>. Distinct
/// from the "contact card" content of an inbound "contacts" message type.
/// </summary>
internal sealed record WebhookContactWire
{
    /// <summary>Gets the WhatsApp id of the user.</summary>
    [JsonPropertyName("wa_id")]
    public string? WaId { get; init; }

    /// <summary>Gets the user's profile information.</summary>
    [JsonPropertyName("profile")]
    public WebhookContactProfileWire? Profile { get; init; }
}

/// <summary>
/// The profile portion of a <see cref="WebhookContactWire"/>.
/// </summary>
internal sealed record WebhookContactProfileWire
{
    /// <summary>Gets the user's display name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}
