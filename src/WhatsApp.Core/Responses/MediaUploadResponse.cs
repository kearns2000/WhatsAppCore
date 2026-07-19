namespace WhatsApp.Core.Responses;

/// <summary>
/// The result of successfully uploading a media asset to the WhatsApp Cloud API.
/// </summary>
public sealed class MediaUploadResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MediaUploadResponse"/> class.
    /// </summary>
    /// <param name="mediaId">The id assigned to the uploaded media asset.</param>
    /// <param name="metadata">HTTP-level metadata captured from the response.</param>
    public MediaUploadResponse(string mediaId, WhatsAppResponseMetadata metadata)
    {
        MediaId = mediaId;
        Metadata = metadata;
    }

    /// <summary>
    /// Gets the id assigned to the uploaded media asset. Use this id in subsequent message
    /// requests (e.g. <see cref="Messages.ImageMessageRequest.MediaId"/>) or with
    /// <see cref="Client.IWhatsAppClient.GetMediaAsync"/>.
    /// </summary>
    public string MediaId { get; }

    /// <summary>
    /// Gets HTTP-level metadata captured alongside this response.
    /// </summary>
    public WhatsAppResponseMetadata Metadata { get; }
}
