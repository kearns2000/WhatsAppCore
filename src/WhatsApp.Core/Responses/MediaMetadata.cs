namespace WhatsApp.Core.Responses;

/// <summary>
/// Metadata describing a previously uploaded (or inbound) media asset, including a short-lived
/// URL from which it may be downloaded.
/// </summary>
/// <remarks>
/// The <see cref="Url"/> is short-lived and specific to this metadata fetch; always request
/// fresh metadata via <see cref="Client.IWhatsAppClient.GetMediaAsync"/> immediately before
/// downloading rather than caching and reusing a previously observed URL.
/// </remarks>
public sealed class MediaMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MediaMetadata"/> class.
    /// </summary>
    /// <param name="mediaId">The id of the media asset.</param>
    /// <param name="url">The short-lived URL from which the media may be downloaded.</param>
    /// <param name="mimeType">The MIME type of the media asset.</param>
    /// <param name="sha256">The SHA-256 checksum of the media asset, if provided.</param>
    /// <param name="fileSizeBytes">The size of the media asset in bytes, if provided.</param>
    /// <param name="metadata">HTTP-level metadata captured from the response.</param>
    public MediaMetadata(
        string mediaId,
        string url,
        string mimeType,
        string? sha256,
        long? fileSizeBytes,
        WhatsAppResponseMetadata metadata)
    {
        MediaId = mediaId;
        Url = url;
        MimeType = mimeType;
        Sha256 = sha256;
        FileSizeBytes = fileSizeBytes;
        Metadata = metadata;
    }

    /// <summary>
    /// Gets the id of the media asset.
    /// </summary>
    public string MediaId { get; }

    /// <summary>
    /// Gets the short-lived URL from which the media may be downloaded. This URL requires the
    /// same bearer access token used for other Graph API calls.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Gets the MIME type of the media asset.
    /// </summary>
    public string MimeType { get; }

    /// <summary>
    /// Gets the SHA-256 checksum of the media asset, if provided by the API.
    /// </summary>
    public string? Sha256 { get; }

    /// <summary>
    /// Gets the size of the media asset in bytes, if provided by the API.
    /// </summary>
    public long? FileSizeBytes { get; }

    /// <summary>
    /// Gets HTTP-level metadata captured alongside this response.
    /// </summary>
    public WhatsAppResponseMetadata Metadata { get; }
}
