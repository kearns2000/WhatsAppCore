using System.Text.Json.Nodes;
using WhatsApp.Core.Internal;

namespace WhatsApp.Core.Messages;

/// <summary>
/// An image message, referencing media either by previously uploaded id or by a publicly
/// reachable link.
/// </summary>
public sealed record ImageMessageRequest : WhatsAppMessageRequest
{
    /// <summary>
    /// Gets the previously uploaded media id. Exactly one of this or <see cref="Link"/> must be set.
    /// </summary>
    public string? MediaId { get; init; }

    /// <summary>
    /// Gets the publicly reachable image URL. Exactly one of this or <see cref="MediaId"/> must be set.
    /// </summary>
    public string? Link { get; init; }

    /// <summary>
    /// Gets the optional caption displayed alongside the image.
    /// </summary>
    public string? Caption { get; init; }

    internal override string Type => "image";

    internal override void Validate() => MediaReferenceValidator.Validate(MediaId, Link, "image");

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
