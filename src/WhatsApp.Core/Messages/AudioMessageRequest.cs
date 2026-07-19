using System.Text.Json.Nodes;
using WhatsApp.Core.Internal;

namespace WhatsApp.Core.Messages;

/// <summary>
/// An audio message, referencing media either by previously uploaded id or by a publicly
/// reachable link. The Graph API does not support captions for audio messages.
/// </summary>
public sealed record AudioMessageRequest : WhatsAppMessageRequest
{
    /// <summary>
    /// Gets the previously uploaded media id. Exactly one of this or <see cref="Link"/> must be set.
    /// </summary>
    public string? MediaId { get; init; }

    /// <summary>
    /// Gets the publicly reachable audio URL. Exactly one of this or <see cref="MediaId"/> must be set.
    /// </summary>
    public string? Link { get; init; }

    internal override string Type => "audio";

    internal override void Validate() => MediaReferenceValidator.Validate(MediaId, Link, "audio");

    internal override JsonNode BuildTypePayload() => MediaId is not null
        ? new JsonObject { ["id"] = MediaId }
        : new JsonObject { ["link"] = Link };
}
