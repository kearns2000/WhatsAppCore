using System.Text.Json.Nodes;
using WhatsApp.Core.Internal;

namespace WhatsApp.Core.Messages;

/// <summary>
/// A video message, referencing media either by previously uploaded id or by a publicly
/// reachable link.
/// </summary>
public sealed record VideoMessageRequest : WhatsAppMessageRequest
{
    /// <summary>
    /// Gets the previously uploaded media id. Exactly one of this or <see cref="Link"/> must be set.
    /// </summary>
    public string? MediaId { get; init; }

    /// <summary>
    /// Gets the publicly reachable video URL. Exactly one of this or <see cref="MediaId"/> must be set.
    /// </summary>
    public string? Link { get; init; }

    /// <summary>
    /// Gets the optional caption displayed alongside the video.
    /// </summary>
    public string? Caption { get; init; }

    internal override string Type => "video";

    internal override void Validate() => MediaReferenceValidator.Validate(MediaId, Link, "video");

    internal override JsonNode BuildTypePayload()
    {
        var obj = new JsonObject();
        if (MediaId is not null)
        {
            obj["id"] = MediaId;
        }
        else
        {
            obj["link"] = Link;
        }

        if (!string.IsNullOrEmpty(Caption))
        {
            obj["caption"] = Caption;
        }

        return obj;
    }
}
