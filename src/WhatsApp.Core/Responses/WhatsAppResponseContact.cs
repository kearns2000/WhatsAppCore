using System.Text.Json;
using System.Text.Json.Serialization;

namespace WhatsApp.Core.Responses;

/// <summary>
/// Identifies a recipient contact resolved by the Graph API when a message is sent.
/// </summary>
public sealed record WhatsAppResponseContact
{
    /// <summary>
    /// Gets the recipient identifier exactly as it was supplied in the request.
    /// </summary>
    [JsonPropertyName("input")]
    public required string Input { get; init; }

    /// <summary>
    /// Gets the WhatsApp user id (<c>wa_id</c>) that the recipient was resolved to, if available.
    /// </summary>
    [JsonPropertyName("wa_id")]
    public string? WaId { get; init; }

    /// <summary>Unrecognized members, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}
