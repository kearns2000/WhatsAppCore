namespace WhatsApp.Core.Media;

/// <summary>
/// A streamed download of a WhatsApp media asset. Owns the underlying HTTP response and its
/// content stream; both are released when this instance is disposed.
/// </summary>
/// <remarks>
/// <see cref="Content"/> is a live, forward-only network stream - the media is not buffered into
/// memory. Callers should read it promptly and dispose this instance (via
/// <see cref="IAsyncDisposable"/>) as soon as they are done, typically within a
/// <c>using</c>/<c>await using</c> block.
/// </remarks>
public sealed class WhatsAppMediaDownload : IAsyncDisposable
{
    private readonly HttpResponseMessage _response;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppMediaDownload"/> class.
    /// </summary>
    /// <param name="response">The underlying HTTP response, disposed together with this instance.</param>
    /// <param name="content">The response content stream.</param>
    /// <param name="contentType">The MIME type of the media.</param>
    /// <param name="contentLength">The size of the media in bytes, if known.</param>
    /// <param name="fileName">The suggested file name for the media, if known.</param>
    public WhatsAppMediaDownload(
        HttpResponseMessage response,
        Stream content,
        string contentType,
        long? contentLength,
        string? fileName)
    {
        _response = response;
        Content = content;
        ContentType = contentType;
        ContentLength = contentLength;
        FileName = fileName;
    }

    /// <summary>
    /// Gets the live content stream. The media is streamed directly from the network and is not
    /// buffered; read it promptly and do not retain it beyond disposal of this instance.
    /// </summary>
    public Stream Content { get; }

    /// <summary>
    /// Gets the MIME type of the media asset.
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    /// Gets the size of the media asset in bytes, if reported by the server.
    /// </summary>
    public long? ContentLength { get; }

    /// <summary>
    /// Gets the suggested file name for the media asset, if reported by the server.
    /// </summary>
    public string? FileName { get; }

    /// <summary>
    /// Releases the content stream and the underlying HTTP response.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await Content.DisposeAsync().ConfigureAwait(false);
        _response.Dispose();
    }
}
