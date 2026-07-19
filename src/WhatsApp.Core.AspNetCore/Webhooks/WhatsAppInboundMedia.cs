namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// Describes a media asset (image, document, audio, video, or sticker) attached to an inbound
/// message. Contains only the media id and metadata Meta includes in the webhook payload; call
/// <c>IWhatsAppClient.GetMediaAsync</c>/<c>DownloadMediaAsync</c> with <see cref="MediaId"/> to
/// retrieve the content itself.
/// </summary>
public sealed record WhatsAppInboundMedia
{
    /// <summary>
    /// Gets the Meta-assigned id of this media asset, used to fetch its metadata or download it.
    /// </summary>
    public required string MediaId { get; init; }

    /// <summary>
    /// Gets the MIME type of the media, if reported.
    /// </summary>
    public string? MimeType { get; init; }

    /// <summary>
    /// Gets the SHA-256 hash of the media content, if reported, useful for integrity checks
    /// after downloading.
    /// </summary>
    public string? Sha256Hash { get; init; }

    /// <summary>
    /// Gets the caption the sender attached to the media, if any. Not present for stickers.
    /// </summary>
    public string? Caption { get; init; }

    /// <summary>
    /// Gets the original filename of the media, if reported (typically only for documents).
    /// </summary>
    public string? FileName { get; init; }
}
