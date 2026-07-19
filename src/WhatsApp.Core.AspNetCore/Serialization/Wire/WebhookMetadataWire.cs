using System.Text.Json;
using System.Text.Json.Serialization;

namespace WhatsApp.Core.AspNetCore.Serialization.Wire;

/// <summary>
/// Identifies which of the application's business phone numbers a change concerns.
/// </summary>
internal sealed record WebhookMetadataWire
{
    /// <summary>Gets the human-readable display phone number.</summary>
    [JsonPropertyName("display_phone_number")]
    public string? DisplayPhoneNumber { get; init; }

    /// <summary>Gets the Meta-assigned phone number id.</summary>
    [JsonPropertyName("phone_number_id")]
    public string? PhoneNumberId { get; init; }

    /// <summary>Unrecognized members, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}
