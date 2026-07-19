using System.Text.Json;
using System.Text.Json.Serialization;

namespace WhatsApp.Core.Serialization.Wire;

/// <summary>
/// The raw JSON body returned by the Graph API when media metadata is retrieved.
/// </summary>
internal sealed record MediaMetadataWireResponse
{
    /// <summary>Gets the constant <c>"whatsapp"</c> product identifier echoed back by the API.</summary>
    [JsonPropertyName("messaging_product")]
    public string? MessagingProduct { get; init; }

    /// <summary>Gets the short-lived URL from which the media may be downloaded.</summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>Gets the MIME type of the media asset.</summary>
    [JsonPropertyName("mime_type")]
    public required string MimeType { get; init; }

    /// <summary>Gets the SHA-256 checksum of the media asset, if provided.</summary>
    [JsonPropertyName("sha256")]
    public string? Sha256 { get; init; }

    /// <summary>Gets the size of the media asset in bytes, if provided.</summary>
    [JsonPropertyName("file_size")]
    public long? FileSize { get; init; }

    /// <summary>Gets the id of the media asset.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>Unrecognized members, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}
