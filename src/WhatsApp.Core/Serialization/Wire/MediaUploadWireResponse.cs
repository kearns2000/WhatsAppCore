using System.Text.Json;
using System.Text.Json.Serialization;

namespace WhatsApp.Core.Serialization.Wire;

/// <summary>
/// The raw JSON body returned by the Graph API when media is uploaded.
/// </summary>
internal sealed record MediaUploadWireResponse
{
    /// <summary>Gets the id assigned to the uploaded media asset.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>Unrecognized members, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}
