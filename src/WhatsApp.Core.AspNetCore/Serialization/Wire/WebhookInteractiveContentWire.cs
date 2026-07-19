using System.Text.Json.Serialization;

namespace WhatsApp.Core.AspNetCore.Serialization.Wire;

/// <summary>The content of an <c>"interactive"</c> reply message.</summary>
internal sealed record WebhookInteractiveContentWire
{
    /// <summary>Gets the interactive reply subtype, e.g. <c>"button_reply"</c> or <c>"list_reply"</c>.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>Gets the button reply details, when <see cref="Type"/> is <c>"button_reply"</c>.</summary>
    [JsonPropertyName("button_reply")]
    public WebhookInteractiveReplyDetailWire? ButtonReply { get; init; }

    /// <summary>Gets the list reply details, when <see cref="Type"/> is <c>"list_reply"</c>.</summary>
    [JsonPropertyName("list_reply")]
    public WebhookInteractiveReplyDetailWire? ListReply { get; init; }
}

/// <summary>The shared shape of <c>button_reply</c> and <c>list_reply</c> details.</summary>
internal sealed record WebhookInteractiveReplyDetailWire
{
    /// <summary>Gets the id of the selected button or list row.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>Gets the display title of the selected button or list row.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>Gets the description of the selected list row, if any.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }
}
