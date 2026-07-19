using System.Text.Json.Serialization;

namespace WhatsApp.Core.AspNetCore.Serialization.Wire;

/// <summary>
/// An error reported by Meta against a specific message or status update.
/// </summary>
internal sealed record WebhookErrorWire
{
    /// <summary>Gets the Graph API error code.</summary>
    [JsonPropertyName("code")]
    public int? Code { get; init; }

    /// <summary>Gets a short error title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>Gets a human-readable error message.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <summary>Gets additional structured error data.</summary>
    [JsonPropertyName("error_data")]
    public WebhookErrorDataWire? ErrorData { get; init; }
}

/// <summary>
/// The <c>error_data</c> portion of a <see cref="WebhookErrorWire"/>.
/// </summary>
internal sealed record WebhookErrorDataWire
{
    /// <summary>Gets additional human-readable error details.</summary>
    [JsonPropertyName("details")]
    public string? Details { get; init; }
}
