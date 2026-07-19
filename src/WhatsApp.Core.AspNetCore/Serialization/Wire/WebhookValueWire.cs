using System.Text.Json;
using System.Text.Json.Serialization;

namespace WhatsApp.Core.AspNetCore.Serialization.Wire;

/// <summary>
/// The <c>value</c> object of a <see cref="WebhookChangeWire"/>. Messages and statuses are kept
/// as raw <see cref="JsonElement"/> here (rather than immediately deserialized) so that the
/// parser can both extract a strongly-typed shape and retain the exact original JSON for
/// forward-compatible <c>ExtensionData</c> on the resulting event.
/// </summary>
internal sealed record WebhookValueWire
{
    /// <summary>Gets the constant <c>"whatsapp"</c> product identifier.</summary>
    [JsonPropertyName("messaging_product")]
    public string? MessagingProduct { get; init; }

    /// <summary>Gets the metadata describing which business phone number this change concerns.</summary>
    [JsonPropertyName("metadata")]
    public WebhookMetadataWire? Metadata { get; init; }

    /// <summary>Gets the WhatsApp user profile(s) associated with the messages in this change.</summary>
    [JsonPropertyName("contacts")]
    public IReadOnlyList<WebhookContactWire>? Contacts { get; init; }

    /// <summary>Gets the raw inbound message objects in this change, if any.</summary>
    [JsonPropertyName("messages")]
    public IReadOnlyList<JsonElement>? Messages { get; init; }

    /// <summary>Gets the raw message status objects in this change, if any.</summary>
    [JsonPropertyName("statuses")]
    public IReadOnlyList<JsonElement>? Statuses { get; init; }

    /// <summary>Gets change-level errors reported by Meta, if any.</summary>
    [JsonPropertyName("errors")]
    public IReadOnlyList<WebhookErrorWire>? Errors { get; init; }

    /// <summary>Unrecognized members, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}
