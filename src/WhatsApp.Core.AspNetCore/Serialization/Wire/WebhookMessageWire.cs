using System.Text.Json.Serialization;

namespace WhatsApp.Core.AspNetCore.Serialization.Wire;

/// <summary>
/// A single inbound message, deserialized from one element of <c>value.messages[]</c>.
/// Exactly one of the content properties (<see cref="Text"/>, <see cref="Image"/>, etc.) is
/// populated, selected by <see cref="Type"/>; unrecognized message types leave all of them
/// <see langword="null"/>, in which case the parser falls back to the original
/// <see cref="System.Text.Json.JsonElement"/> it was deserialized from.
/// </summary>
internal sealed record WebhookMessageWire
{
    /// <summary>Gets the WhatsApp id of the sender.</summary>
    [JsonPropertyName("from")]
    public string? From { get; init; }

    /// <summary>Gets the WhatsApp message id.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>Gets the Unix timestamp (seconds) at which the message was sent.</summary>
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; init; }

    /// <summary>Gets the message type discriminator, e.g. <c>"text"</c>, <c>"image"</c>.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>Gets the message this message replies to, if any.</summary>
    [JsonPropertyName("context")]
    public WebhookMessageContextWire? Context { get; init; }

    /// <summary>Gets the text content, when <see cref="Type"/> is <c>"text"</c>.</summary>
    [JsonPropertyName("text")]
    public WebhookTextContentWire? Text { get; init; }

    /// <summary>Gets the image content, when <see cref="Type"/> is <c>"image"</c>.</summary>
    [JsonPropertyName("image")]
    public WebhookMediaContentWire? Image { get; init; }

    /// <summary>Gets the document content, when <see cref="Type"/> is <c>"document"</c>.</summary>
    [JsonPropertyName("document")]
    public WebhookMediaContentWire? Document { get; init; }

    /// <summary>Gets the audio content, when <see cref="Type"/> is <c>"audio"</c>.</summary>
    [JsonPropertyName("audio")]
    public WebhookAudioContentWire? Audio { get; init; }

    /// <summary>Gets the video content, when <see cref="Type"/> is <c>"video"</c>.</summary>
    [JsonPropertyName("video")]
    public WebhookMediaContentWire? Video { get; init; }

    /// <summary>Gets the sticker content, when <see cref="Type"/> is <c>"sticker"</c>.</summary>
    [JsonPropertyName("sticker")]
    public WebhookStickerContentWire? Sticker { get; init; }

    /// <summary>Gets the location content, when <see cref="Type"/> is <c>"location"</c>.</summary>
    [JsonPropertyName("location")]
    public WebhookLocationContentWire? Location { get; init; }

    /// <summary>Gets the shared contact cards, when <see cref="Type"/> is <c>"contacts"</c>.</summary>
    [JsonPropertyName("contacts")]
    public IReadOnlyList<WebhookMessageContactCardWire>? Contacts { get; init; }

    /// <summary>Gets the interactive reply content, when <see cref="Type"/> is <c>"interactive"</c>.</summary>
    [JsonPropertyName("interactive")]
    public WebhookInteractiveContentWire? Interactive { get; init; }

    /// <summary>Gets the quick-reply button content, when <see cref="Type"/> is <c>"button"</c>.</summary>
    [JsonPropertyName("button")]
    public WebhookButtonContentWire? Button { get; init; }

    /// <summary>Gets the reaction content, when <see cref="Type"/> is <c>"reaction"</c>.</summary>
    [JsonPropertyName("reaction")]
    public WebhookReactionContentWire? Reaction { get; init; }
}

/// <summary>The <c>context</c> object identifying a message being replied to.</summary>
internal sealed record WebhookMessageContextWire
{
    /// <summary>Gets the WhatsApp message id being replied to.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>Gets the WhatsApp id of the sender of the original message.</summary>
    [JsonPropertyName("from")]
    public string? From { get; init; }
}

/// <summary>The content of a <c>"text"</c> message.</summary>
internal sealed record WebhookTextContentWire
{
    /// <summary>Gets the text body.</summary>
    [JsonPropertyName("body")]
    public string? Body { get; init; }
}

/// <summary>The content of an <c>"image"</c>, <c>"document"</c>, or <c>"video"</c> message.</summary>
internal sealed record WebhookMediaContentWire
{
    /// <summary>Gets the media id.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>Gets the media MIME type.</summary>
    [JsonPropertyName("mime_type")]
    public string? MimeType { get; init; }

    /// <summary>Gets the media SHA-256 hash.</summary>
    [JsonPropertyName("sha256")]
    public string? Sha256 { get; init; }

    /// <summary>Gets the caption attached to the media, if any.</summary>
    [JsonPropertyName("caption")]
    public string? Caption { get; init; }

    /// <summary>Gets the original filename, if reported (documents only).</summary>
    [JsonPropertyName("filename")]
    public string? Filename { get; init; }
}

/// <summary>The content of an <c>"audio"</c> message.</summary>
internal sealed record WebhookAudioContentWire
{
    /// <summary>Gets the media id.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>Gets the media MIME type.</summary>
    [JsonPropertyName("mime_type")]
    public string? MimeType { get; init; }

    /// <summary>Gets the media SHA-256 hash.</summary>
    [JsonPropertyName("sha256")]
    public string? Sha256 { get; init; }

    /// <summary>Gets a value indicating whether this is a voice note recorded in WhatsApp.</summary>
    [JsonPropertyName("voice")]
    public bool? Voice { get; init; }
}

/// <summary>The content of a <c>"sticker"</c> message.</summary>
internal sealed record WebhookStickerContentWire
{
    /// <summary>Gets the media id.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>Gets the media MIME type.</summary>
    [JsonPropertyName("mime_type")]
    public string? MimeType { get; init; }

    /// <summary>Gets the media SHA-256 hash.</summary>
    [JsonPropertyName("sha256")]
    public string? Sha256 { get; init; }

    /// <summary>Gets a value indicating whether the sticker is animated.</summary>
    [JsonPropertyName("animated")]
    public bool? Animated { get; init; }
}

/// <summary>The content of a <c>"location"</c> message.</summary>
internal sealed record WebhookLocationContentWire
{
    /// <summary>Gets the latitude.</summary>
    [JsonPropertyName("latitude")]
    public double? Latitude { get; init; }

    /// <summary>Gets the longitude.</summary>
    [JsonPropertyName("longitude")]
    public double? Longitude { get; init; }

    /// <summary>Gets the location name, if provided.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Gets the location's street address, if provided.</summary>
    [JsonPropertyName("address")]
    public string? Address { get; init; }
}

/// <summary>The content of a <c>"button"</c> (quick-reply) message.</summary>
internal sealed record WebhookButtonContentWire
{
    /// <summary>Gets the display text of the tapped button.</summary>
    [JsonPropertyName("text")]
    public string? Text { get; init; }

    /// <summary>Gets the payload configured for the tapped button.</summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; init; }
}

/// <summary>The content of a <c>"reaction"</c> message.</summary>
internal sealed record WebhookReactionContentWire
{
    /// <summary>Gets the WhatsApp message id being reacted to.</summary>
    [JsonPropertyName("message_id")]
    public string? MessageId { get; init; }

    /// <summary>Gets the emoji used, or an empty string when a reaction was removed.</summary>
    [JsonPropertyName("emoji")]
    public string? Emoji { get; init; }
}
