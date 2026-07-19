using System.Text.Json;
using System.Text.Json.Serialization;

namespace WhatsApp.Core.Responses;

/// <summary>
/// Identifies a single message accepted by the Graph API.
/// </summary>
public sealed record WhatsAppMessageId
{
    /// <summary>
    /// Gets the WhatsApp message id (e.g. <c>wamid.HBgL...</c>) assigned to the accepted message.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Gets the message status reported by the API, if any (e.g. <c>accepted</c>).
    /// </summary>
    [JsonPropertyName("message_status")]
    public string? MessageStatus { get; init; }

    /// <summary>Unrecognized members, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}
