using System.Text.Json.Nodes;
using WhatsApp.Core.Errors;
using WhatsApp.Core.Media;
using WhatsApp.Core.Messages;
using WhatsApp.Core.Responses;

namespace WhatsApp.Core.Client;

/// <summary>
/// A client for a single WhatsApp Business Account, exposing the WhatsApp Cloud API's messaging
/// and media operations.
/// </summary>
/// <remarks>
/// Obtain instances via dependency injection (a registered <see cref="IWhatsAppClient"/> for the
/// default account, or <see cref="IWhatsAppClientFactory"/> for named accounts) rather than
/// constructing implementations directly. Implementations are safe to use concurrently from
/// multiple threads.
/// </remarks>
public interface IWhatsAppClient
{
    /// <summary>
    /// Gets the logical account name this client is bound to.
    /// </summary>
    string AccountName { get; }

    /// <summary>
    /// Sends a message built from a strongly-typed <see cref="WhatsAppMessageRequest"/>. The
    /// request is validated client-side before any network call is made.
    /// </summary>
    /// <param name="request">The message request to send.</param>
    /// <param name="stopToken">A token used to cancel the operation.</param>
    /// <returns>The response describing the accepted message.</returns>
    /// <exception cref="WhatsAppValidationException">The request failed client-side validation.</exception>
    /// <exception cref="WhatsAppApiException">The Graph API rejected the request.</exception>
    Task<SendMessageResponse> SendMessageAsync(WhatsAppMessageRequest request, CancellationToken stopToken = default);

    /// <summary>
    /// Sends a message from a raw JSON payload, for message shapes not yet modeled by this
    /// library. The payload is always posted to this client's configured <c>/messages</c>
    /// endpoint; nothing in the payload can redirect the request elsewhere. The
    /// <c>messaging_product</c> field is set to <c>"whatsapp"</c> automatically if omitted.
    /// </summary>
    /// <param name="payload">The raw message payload, minus <c>messaging_product</c> if it should be defaulted.</param>
    /// <param name="stopToken">A token used to cancel the operation.</param>
    /// <returns>The response describing the accepted message.</returns>
    /// <exception cref="WhatsAppValidationException">The payload is missing required fields.</exception>
    /// <exception cref="WhatsAppApiException">The Graph API rejected the request.</exception>
    Task<SendMessageResponse> SendRawMessageAsync(JsonObject payload, CancellationToken stopToken = default);

    /// <summary>
    /// Marks a previously received message as read, causing double blue check marks to appear
    /// in the sender's client.
    /// </summary>
    /// <param name="messageId">The WhatsApp message id (<c>wamid...</c>) to mark as read.</param>
    /// <param name="stopToken">A token used to cancel the operation.</param>
    /// <exception cref="WhatsAppApiException">The Graph API rejected the request.</exception>
    Task MarkMessageAsReadAsync(string messageId, CancellationToken stopToken = default);

    /// <summary>
    /// Uploads a media asset for later use in messages, returning the media id to reference in
    /// subsequent send calls.
    /// </summary>
    /// <param name="content">The media content stream. Read fully but never buffered entirely into memory by this client. The caller retains ownership; this method does not dispose the stream.</param>
    /// <param name="fileName">The file name to associate with the upload.</param>
    /// <param name="contentType">The MIME type of the media.</param>
    /// <param name="stopToken">A token used to cancel the operation.</param>
    /// <returns>The response containing the uploaded media's id.</returns>
    /// <exception cref="WhatsAppApiException">The Graph API rejected the request.</exception>
    Task<MediaUploadResponse> UploadMediaAsync(Stream content, string fileName, string contentType, CancellationToken stopToken = default);

    /// <summary>
    /// Retrieves metadata for a media asset, including a short-lived download URL. Always
    /// fetches fresh metadata from the Graph API; the resulting URL should be used immediately
    /// and never cached.
    /// </summary>
    /// <param name="mediaId">The id of the media asset.</param>
    /// <param name="stopToken">A token used to cancel the operation.</param>
    /// <returns>The media's metadata.</returns>
    /// <exception cref="WhatsAppApiException">The Graph API rejected the request.</exception>
    Task<MediaMetadata> GetMediaAsync(string mediaId, CancellationToken stopToken = default);

    /// <summary>
    /// Downloads a media asset as a stream. Always fetches fresh metadata immediately before
    /// downloading (see <see cref="GetMediaAsync"/>), rather than relying on a previously
    /// observed, potentially expired URL. The returned content is streamed, not buffered.
    /// </summary>
    /// <param name="mediaId">The id of the media asset.</param>
    /// <param name="stopToken">A token used to cancel the operation.</param>
    /// <returns>The streamed media download. Dispose it (it is <see cref="IAsyncDisposable"/>) once finished reading.</returns>
    /// <exception cref="WhatsAppApiException">The Graph API rejected the request.</exception>
    Task<WhatsAppMediaDownload> DownloadMediaAsync(string mediaId, CancellationToken stopToken = default);

    /// <summary>
    /// Deletes a previously uploaded media asset.
    /// </summary>
    /// <param name="mediaId">The id of the media asset to delete.</param>
    /// <param name="stopToken">A token used to cancel the operation.</param>
    /// <exception cref="WhatsAppApiException">The Graph API rejected the request.</exception>
    Task DeleteMediaAsync(string mediaId, CancellationToken stopToken = default);
}
