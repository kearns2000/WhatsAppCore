using System.Text.Json;
using System.Text.Json.Serialization;

namespace WhatsApp.Core.AspNetCore.Serialization.Wire;

/// <summary>
/// A single change reported within a <see cref="WebhookEntryWire"/>. This library only acts on
/// changes whose <see cref="Value"/> carries <c>messages</c> or <c>statuses</c>; other fields
/// (e.g. account or template review updates) are ignored.
/// </summary>
internal sealed record WebhookChangeWire
{
    /// <summary>Gets the field this change applies to, e.g. <c>"messages"</c>.</summary>
    [JsonPropertyName("field")]
    public string? Field { get; init; }

    /// <summary>Gets the value payload for this change.</summary>
    [JsonPropertyName("value")]
    public WebhookValueWire? Value { get; init; }

    /// <summary>Unrecognized members, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}
