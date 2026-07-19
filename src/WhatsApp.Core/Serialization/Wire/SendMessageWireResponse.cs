using System.Text.Json;
using System.Text.Json.Serialization;
using WhatsApp.Core.Responses;

namespace WhatsApp.Core.Serialization.Wire;

/// <summary>
/// The raw JSON body returned by the Graph API when a message is sent, before the library
/// attaches HTTP-level <see cref="WhatsAppResponseMetadata"/>.
/// </summary>
internal sealed record SendMessageWireResponse
{
    /// <summary>Gets the constant <c>"whatsapp"</c> product identifier echoed back by the API.</summary>
    [JsonPropertyName("messaging_product")]
    public string? MessagingProduct { get; init; }

    /// <summary>Gets the recipient contacts resolved by the API.</summary>
    [JsonPropertyName("contacts")]
    public IReadOnlyList<WhatsAppResponseContact>? Contacts { get; init; }

    /// <summary>Gets the ids of the messages that were accepted.</summary>
    [JsonPropertyName("messages")]
    public IReadOnlyList<WhatsAppMessageId>? Messages { get; init; }

    /// <summary>Unrecognized members, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}
