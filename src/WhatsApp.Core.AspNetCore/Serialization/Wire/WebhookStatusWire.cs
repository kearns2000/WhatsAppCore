using System.Text.Json.Serialization;

namespace WhatsApp.Core.AspNetCore.Serialization.Wire;

/// <summary>
/// A single message status update, deserialized from one element of <c>value.statuses[]</c>.
/// </summary>
internal sealed record WebhookStatusWire
{
    /// <summary>Gets the WhatsApp message id this status update describes.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>Gets the status value, e.g. <c>"sent"</c>, <c>"delivered"</c>, <c>"read"</c>, <c>"failed"</c>.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; init; }

    /// <summary>Gets the Unix timestamp (seconds) at which this status transition occurred.</summary>
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; init; }

    /// <summary>Gets the WhatsApp id of the message's recipient.</summary>
    [JsonPropertyName("recipient_id")]
    public string? RecipientId { get; init; }

    /// <summary>Gets the errors reported for a <c>"failed"</c> status.</summary>
    [JsonPropertyName("errors")]
    public IReadOnlyList<WebhookErrorWire>? Errors { get; init; }
}
