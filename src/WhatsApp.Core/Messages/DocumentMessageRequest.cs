using System.Text.Json.Nodes;
using WhatsApp.Core.Internal;

namespace WhatsApp.Core.Messages;

/// <summary>
/// A document message, referencing media either by previously uploaded id or by a publicly
/// reachable link.
/// </summary>
public sealed record DocumentMessageRequest : WhatsAppMessageRequest
{
    /// <summary>
    /// Gets the previously uploaded media id. Exactly one of this or <see cref="Link"/> must be set.
    /// </summary>
    public string? MediaId { get; init; }

    /// <summary>
    /// Gets the publicly reachable document URL. Exactly one of this or <see cref="MediaId"/> must be set.
    /// </summary>
    public string? Link { get; init; }

    /// <summary>
    /// Gets the optional caption displayed alongside the document.
    /// </summary>
    public string? Caption { get; init; }

    /// <summary>
    /// Gets the optional file name suggested to the recipient (e.g. <c>"invoice.pdf"</c>).
    /// </summary>
    public string? FileName { get; init; }

    internal override string Type => "document";

    internal override void Validate() => MediaReferenceValidator.Validate(MediaId, Link, "document");

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

        if (!string.IsNullOrEmpty(FileName))
        {
            obj["filename"] = FileName;
        }

        return obj;
    }
}
