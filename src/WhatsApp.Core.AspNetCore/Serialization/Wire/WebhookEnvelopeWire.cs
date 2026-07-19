using System.Text.Json;
using System.Text.Json.Serialization;

namespace WhatsApp.Core.AspNetCore.Serialization.Wire;

/// <summary>
/// The top-level shape of every WhatsApp Business Account webhook delivery:
/// <c>{ "object": "whatsapp_business_account", "entry": [ ... ] }</c>.
/// </summary>
internal sealed record WebhookEnvelopeWire
{
    /// <summary>Gets the webhook subscription object type, e.g. <c>"whatsapp_business_account"</c>.</summary>
    [JsonPropertyName("object")]
    public string? Object { get; init; }

    /// <summary>Gets the entries in this delivery, one per WhatsApp Business Account.</summary>
    [JsonPropertyName("entry")]
    public IReadOnlyList<WebhookEntryWire>? Entry { get; init; }

    /// <summary>Unrecognized members, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}
