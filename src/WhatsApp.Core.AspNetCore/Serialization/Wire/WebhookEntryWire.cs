using System.Text.Json;
using System.Text.Json.Serialization;

namespace WhatsApp.Core.AspNetCore.Serialization.Wire;

/// <summary>
/// A single entry in a webhook delivery, scoped to one WhatsApp Business Account.
/// </summary>
internal sealed record WebhookEntryWire
{
    /// <summary>Gets the WhatsApp Business Account id this entry reports on.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>Gets the changes reported for this account.</summary>
    [JsonPropertyName("changes")]
    public IReadOnlyList<WebhookChangeWire>? Changes { get; init; }

    /// <summary>Unrecognized members, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}
