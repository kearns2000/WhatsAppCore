using System.Text.Json.Nodes;
using WhatsApp.Core.Internal;

namespace WhatsApp.Core.Messages;

/// <summary>
/// A sticker message, referencing media either by previously uploaded id or by a publicly
/// reachable link. Stickers must be static or animated WebP images.
/// </summary>
public sealed record StickerMessageRequest : WhatsAppMessageRequest
{
    /// <summary>
    /// Gets the previously uploaded media id. Exactly one of this or <see cref="Link"/> must be set.
    /// </summary>
    public string? MediaId { get; init; }

    /// <summary>
    /// Gets the publicly reachable sticker (WebP) URL. Exactly one of this or <see cref="MediaId"/> must be set.
    /// </summary>
    public string? Link { get; init; }

    internal override string Type => "sticker";

    internal override void Validate() => MediaReferenceValidator.Validate(MediaId, Link, "sticker");

    internal override JsonNode BuildTypePayload() => MediaId is not null
        ? new JsonObject { ["id"] = MediaId }
        : new JsonObject { ["link"] = Link };
}
