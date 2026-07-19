using System.Text.Json.Nodes;
using WhatsApp.Core.Errors;
using WhatsApp.Core.Internal;

namespace WhatsApp.Core.Messages;

/// <summary>
/// The optional header of an <see cref="InteractiveMessageRequest"/>. Exactly one of
/// <see cref="Text"/>, an image reference, a video reference, or a document reference must be set.
/// </summary>
public sealed record WhatsAppInteractiveHeader
{
    /// <summary>Gets the header text. Mutually exclusive with the media references below.</summary>
    public string? Text { get; init; }

    /// <summary>Gets the previously uploaded image media id.</summary>
    public string? ImageId { get; init; }

    /// <summary>Gets the publicly reachable image URL.</summary>
    public string? ImageLink { get; init; }

    /// <summary>Gets the previously uploaded video media id.</summary>
    public string? VideoId { get; init; }

    /// <summary>Gets the publicly reachable video URL.</summary>
    public string? VideoLink { get; init; }

    /// <summary>Gets the previously uploaded document media id.</summary>
    public string? DocumentId { get; init; }

    /// <summary>Gets the publicly reachable document URL.</summary>
    public string? DocumentLink { get; init; }

    /// <summary>
    /// Creates a text header.
    /// </summary>
    /// <param name="text">The header text.</param>
    public static WhatsAppInteractiveHeader ForText(string text) => new() { Text = text };

    /// <summary>
    /// Creates an image header referencing media by id or link.
    /// </summary>
    /// <param name="mediaId">The previously uploaded media id.</param>
    /// <param name="link">The publicly reachable media URL.</param>
    public static WhatsAppInteractiveHeader ForImage(string? mediaId = null, string? link = null) =>
        new() { ImageId = mediaId, ImageLink = link };

    /// <summary>
    /// Creates a video header referencing media by id or link.
    /// </summary>
    /// <param name="mediaId">The previously uploaded media id.</param>
    /// <param name="link">The publicly reachable media URL.</param>
    public static WhatsAppInteractiveHeader ForVideo(string? mediaId = null, string? link = null) =>
        new() { VideoId = mediaId, VideoLink = link };

    /// <summary>
    /// Creates a document header referencing media by id or link.
    /// </summary>
    /// <param name="mediaId">The previously uploaded media id.</param>
    /// <param name="link">The publicly reachable media URL.</param>
    public static WhatsAppInteractiveHeader ForDocument(string? mediaId = null, string? link = null) =>
        new() { DocumentId = mediaId, DocumentLink = link };

    internal void Validate()
    {
        var hasText = !string.IsNullOrEmpty(Text);
        var hasImage = ImageId is not null || ImageLink is not null;
        var hasVideo = VideoId is not null || VideoLink is not null;
        var hasDocument = DocumentId is not null || DocumentLink is not null;

        var setCount = (hasText ? 1 : 0) + (hasImage ? 1 : 0) + (hasVideo ? 1 : 0) + (hasDocument ? 1 : 0);
        if (setCount != 1)
        {
            throw new WhatsAppValidationException(
                "An interactive header must specify exactly one of text, an image, a video, or a document.");
        }

        if (hasImage)
        {
            MediaReferenceValidator.Validate(ImageId, ImageLink, "interactive header image");
        }

        if (hasVideo)
        {
            MediaReferenceValidator.Validate(VideoId, VideoLink, "interactive header video");
        }

        if (hasDocument)
        {
            MediaReferenceValidator.Validate(DocumentId, DocumentLink, "interactive header document");
        }
    }

    internal JsonObject ToJson()
    {
        if (!string.IsNullOrEmpty(Text))
        {
            return new JsonObject { ["type"] = "text", ["text"] = Text };
        }

        if (ImageId is not null || ImageLink is not null)
        {
            return new JsonObject { ["type"] = "image", ["image"] = MediaRef(ImageId, ImageLink) };
        }

        if (VideoId is not null || VideoLink is not null)
        {
            return new JsonObject { ["type"] = "video", ["video"] = MediaRef(VideoId, VideoLink) };
        }

        return new JsonObject { ["type"] = "document", ["document"] = MediaRef(DocumentId, DocumentLink) };
    }

    private static JsonObject MediaRef(string? id, string? link)
    {
        var obj = new JsonObject();
        if (id is not null)
        {
            obj["id"] = id;
        }
        else
        {
            obj["link"] = link;
        }

        return obj;
    }
}

/// <summary>
/// The body text of an <see cref="InteractiveMessageRequest"/>.
/// </summary>
public sealed record WhatsAppInteractiveBody
{
    /// <summary>Gets the body text.</summary>
    public required string Text { get; init; }
}

/// <summary>
/// The footer text of an <see cref="InteractiveMessageRequest"/>.
/// </summary>
public sealed record WhatsAppInteractiveFooter
{
    /// <summary>Gets the footer text.</summary>
    public required string Text { get; init; }
}
