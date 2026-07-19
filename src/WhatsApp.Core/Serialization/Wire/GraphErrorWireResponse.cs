using System.Text.Json;
using System.Text.Json.Serialization;

namespace WhatsApp.Core.Serialization.Wire;

/// <summary>
/// The top-level envelope Meta uses to report errors: <c>{ "error": { ... } }</c>.
/// </summary>
internal sealed record GraphErrorWireResponse
{
    /// <summary>Gets the structured error details.</summary>
    [JsonPropertyName("error")]
    public GraphErrorDetailWire? Error { get; init; }

    /// <summary>Unrecognized members, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>
/// The structured error details reported by the Meta Graph API.
/// </summary>
internal sealed record GraphErrorDetailWire
{
    /// <summary>Gets the human-readable error message.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <summary>Gets the Graph API error type, e.g. <c>OAuthException</c>.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>Gets the top-level numeric error code.</summary>
    [JsonPropertyName("code")]
    public int? Code { get; init; }

    /// <summary>Gets the more specific numeric error subcode.</summary>
    [JsonPropertyName("error_subcode")]
    public int? ErrorSubcode { get; init; }

    /// <summary>Gets Meta's trace id for the request.</summary>
    [JsonPropertyName("fbtrace_id")]
    public string? FbTraceId { get; init; }

    /// <summary>Gets a user-facing error title, if provided.</summary>
    [JsonPropertyName("error_user_title")]
    public string? ErrorUserTitle { get; init; }

    /// <summary>Gets a user-facing error message, if provided.</summary>
    [JsonPropertyName("error_user_msg")]
    public string? ErrorUserMessage { get; init; }

    /// <summary>Gets additional structured error data.</summary>
    [JsonPropertyName("error_data")]
    public JsonElement? ErrorData { get; init; }

    /// <summary>Unrecognized members, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}
