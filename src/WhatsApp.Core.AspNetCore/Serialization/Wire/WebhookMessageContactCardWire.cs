using System.Text.Json.Serialization;

namespace WhatsApp.Core.AspNetCore.Serialization.Wire;

/// <summary>A single contact card shared via a <c>"contacts"</c> message.</summary>
internal sealed record WebhookMessageContactCardWire
{
    /// <summary>Gets the contact's name details.</summary>
    [JsonPropertyName("name")]
    public WebhookContactCardNameWire? Name { get; init; }

    /// <summary>Gets the contact's phone numbers.</summary>
    [JsonPropertyName("phones")]
    public IReadOnlyList<WebhookContactCardPhoneWire>? Phones { get; init; }
}

/// <summary>The <c>name</c> portion of a <see cref="WebhookMessageContactCardWire"/>.</summary>
internal sealed record WebhookContactCardNameWire
{
    /// <summary>Gets the contact's formatted display name.</summary>
    [JsonPropertyName("formatted_name")]
    public string? FormattedName { get; init; }
}

/// <summary>A single phone entry on a <see cref="WebhookMessageContactCardWire"/>.</summary>
internal sealed record WebhookContactCardPhoneWire
{
    /// <summary>Gets the formatted phone number.</summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; init; }

    /// <summary>Gets the WhatsApp id associated with this phone number, if any.</summary>
    [JsonPropertyName("wa_id")]
    public string? WaId { get; init; }

    /// <summary>Gets the label associated with this phone number, e.g. <c>"CELL"</c>.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }
}
