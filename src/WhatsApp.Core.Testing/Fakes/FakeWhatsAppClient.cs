using System.Collections.Concurrent;
using System.Net;
using System.Text.Json.Nodes;
using WhatsApp.Core.Client;
using WhatsApp.Core.Media;
using WhatsApp.Core.Messages;
using WhatsApp.Core.Responses;

namespace WhatsApp.Core.Testing.Fakes;

/// <summary>
/// Identifies the kind of outbound operation recorded by <see cref="FakeWhatsAppClient"/>.
/// </summary>
public enum RecordedWhatsAppOperation
{
    /// <summary>A strongly-typed or raw message send.</summary>
    SendMessage,

    /// <summary>A raw JSON message send.</summary>
    SendRawMessage,

    /// <summary>A mark-as-read request.</summary>
    MarkAsRead,

    /// <summary>A media upload.</summary>
    UploadMedia,

    /// <summary>A media metadata retrieval.</summary>
    GetMedia,

    /// <summary>A media download.</summary>
    DownloadMedia,

    /// <summary>A media delete.</summary>
    DeleteMedia,
}

/// <summary>
/// A recorded outbound request captured by <see cref="FakeWhatsAppClient"/>.
/// </summary>
public sealed class RecordedWhatsAppRequest
{
    /// <summary>Initializes a new recorded request.</summary>
    public RecordedWhatsAppRequest(
        RecordedWhatsAppOperation operation,
        WhatsAppMessageRequest? message = null,
        JsonObject? rawPayload = null,
        string? messageId = null,
        string? mediaId = null,
        string? fileName = null,
        string? contentType = null,
        byte[]? contentCopy = null)
    {
        Operation = operation;
        Message = message;
        RawPayload = rawPayload;
        MessageId = messageId;
        MediaId = mediaId;
        FileName = fileName;
        ContentType = contentType;
        ContentCopy = contentCopy;
    }

    /// <summary>Gets the operation that was invoked.</summary>
    public RecordedWhatsAppOperation Operation { get; }

    /// <summary>Gets the strongly-typed message request, when applicable.</summary>
    public WhatsAppMessageRequest? Message { get; }

    /// <summary>Gets a copy of the raw JSON payload, when applicable.</summary>
    public JsonObject? RawPayload { get; }

    /// <summary>Gets the message id for mark-as-read operations.</summary>
    public string? MessageId { get; }

    /// <summary>Gets the media id for media operations.</summary>
    public string? MediaId { get; }

    /// <summary>Gets the upload file name, when applicable.</summary>
    public string? FileName { get; }

    /// <summary>Gets the upload content type, when applicable.</summary>
    public string? ContentType { get; }

    /// <summary>Gets a buffered copy of uploaded content, when applicable.</summary>
    public byte[]? ContentCopy { get; }
}

/// <summary>
/// A thread-safe fake <see cref="IWhatsAppClient"/> for unit and integration tests.
/// </summary>
public sealed class FakeWhatsAppClient : IWhatsAppClient
{
    private readonly object _gate = new();
    private readonly List<RecordedWhatsAppRequest> _requests = [];
    private readonly ConcurrentQueue<object> _messageResults = new();
    private readonly ConcurrentQueue<object> _mediaUploadResults = new();
    private readonly ConcurrentQueue<object> _mediaMetadataResults = new();
    private readonly ConcurrentQueue<object> _mediaDownloadResults = new();
    private readonly ConcurrentQueue<object> _voidResults = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeWhatsAppClient"/> class.
    /// </summary>
    /// <param name="accountName">The logical account name exposed by <see cref="AccountName"/>.</param>
    public FakeWhatsAppClient(string accountName = "Default")
    {
        AccountName = accountName;
    }

    /// <inheritdoc />
    public string AccountName { get; }

    /// <summary>Gets the recorded outbound requests in invocation order.</summary>
    public IReadOnlyList<RecordedWhatsAppRequest> Requests
    {
        get
        {
            lock (_gate)
            {
                return _requests.ToArray();
            }
        }
    }

    /// <summary>Queues a successful send-message response.</summary>
    public void QueueResponse(SendMessageResponse response) =>
        _messageResults.Enqueue(response ?? throw new ArgumentNullException(nameof(response)));

    /// <summary>Queues an exception for the next send-message or raw-send call.</summary>
    public void QueueException(Exception exception) =>
        _messageResults.Enqueue(exception ?? throw new ArgumentNullException(nameof(exception)));

    /// <summary>Queues a media upload response.</summary>
    public void QueueMediaUploadResponse(MediaUploadResponse response) =>
        _mediaUploadResults.Enqueue(response ?? throw new ArgumentNullException(nameof(response)));

    /// <summary>Queues a media metadata response.</summary>
    public void QueueMediaMetadata(MediaMetadata metadata) =>
        _mediaMetadataResults.Enqueue(metadata ?? throw new ArgumentNullException(nameof(metadata)));

    /// <summary>Queues a media download result.</summary>
    public void QueueMediaDownload(WhatsAppMediaDownload download) =>
        _mediaDownloadResults.Enqueue(download ?? throw new ArgumentNullException(nameof(download)));

    /// <summary>Queues an exception for the next void operation (mark-as-read or delete).</summary>
    public void QueueVoidException(Exception exception) =>
        _voidResults.Enqueue(exception ?? throw new ArgumentNullException(nameof(exception)));

    /// <summary>Queues an exception for the next media upload call.</summary>
    public void QueueMediaUploadException(Exception exception) =>
        _mediaUploadResults.Enqueue(exception ?? throw new ArgumentNullException(nameof(exception)));

    /// <summary>Queues an exception for the next media metadata call.</summary>
    public void QueueMediaMetadataException(Exception exception) =>
        _mediaMetadataResults.Enqueue(exception ?? throw new ArgumentNullException(nameof(exception)));

    /// <summary>Queues an exception for the next media download call.</summary>
    public void QueueMediaDownloadException(Exception exception) =>
        _mediaDownloadResults.Enqueue(exception ?? throw new ArgumentNullException(nameof(exception)));

    /// <summary>Clears recorded requests and queued results.</summary>
    public void Reset()
    {
        lock (_gate)
        {
            _requests.Clear();
        }

        while (_messageResults.TryDequeue(out _))
        {
        }

        while (_mediaUploadResults.TryDequeue(out _))
        {
        }

        while (_mediaMetadataResults.TryDequeue(out _))
        {
        }

        while (_mediaDownloadResults.TryDequeue(out _))
        {
        }

        while (_voidResults.TryDequeue(out _))
        {
        }
    }

    /// <summary>
    /// Throws if any queued responses/exceptions remain, indicating the test left unexpected work.
    /// </summary>
    public void EnsureNoUnusedQueuedResults()
    {
        if (!_messageResults.IsEmpty ||
            !_mediaUploadResults.IsEmpty ||
            !_mediaMetadataResults.IsEmpty ||
            !_mediaDownloadResults.IsEmpty ||
            !_voidResults.IsEmpty)
        {
            throw new InvalidOperationException("FakeWhatsAppClient still has unused queued results.");
        }
    }

    /// <summary>Throws if any requests were recorded.</summary>
    public void EnsureNoRequests()
    {
        if (Requests.Count > 0)
        {
            throw new InvalidOperationException($"Expected no requests but recorded {Requests.Count}.");
        }
    }

    /// <inheritdoc />
    public Task<SendMessageResponse> SendMessageAsync(WhatsAppMessageRequest request, CancellationToken stopToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        stopToken.ThrowIfCancellationRequested();
        Record(new RecordedWhatsAppRequest(RecordedWhatsAppOperation.SendMessage, message: request));
        return Task.FromResult(DequeueMessageResult());
    }

    /// <inheritdoc />
    public Task<SendMessageResponse> SendRawMessageAsync(JsonObject payload, CancellationToken stopToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        stopToken.ThrowIfCancellationRequested();
        Record(new RecordedWhatsAppRequest(
            RecordedWhatsAppOperation.SendRawMessage,
            rawPayload: (JsonObject)payload.DeepClone()));
        return Task.FromResult(DequeueMessageResult());
    }

    /// <inheritdoc />
    public Task MarkMessageAsReadAsync(string messageId, CancellationToken stopToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        stopToken.ThrowIfCancellationRequested();
        Record(new RecordedWhatsAppRequest(RecordedWhatsAppOperation.MarkAsRead, messageId: messageId));
        DequeueVoidResult();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<MediaUploadResponse> UploadMediaAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken stopToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        stopToken.ThrowIfCancellationRequested();

        using var copy = new MemoryStream();
        await content.CopyToAsync(copy, stopToken).ConfigureAwait(false);
        Record(new RecordedWhatsAppRequest(
            RecordedWhatsAppOperation.UploadMedia,
            fileName: fileName,
            contentType: contentType,
            contentCopy: copy.ToArray()));

        return Dequeue<MediaUploadResponse>(_mediaUploadResults, "media upload");
    }

    /// <inheritdoc />
    public Task<MediaMetadata> GetMediaAsync(string mediaId, CancellationToken stopToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaId);
        stopToken.ThrowIfCancellationRequested();
        Record(new RecordedWhatsAppRequest(RecordedWhatsAppOperation.GetMedia, mediaId: mediaId));
        return Task.FromResult(Dequeue<MediaMetadata>(_mediaMetadataResults, "media metadata"));
    }

    /// <inheritdoc />
    public Task<WhatsAppMediaDownload> DownloadMediaAsync(string mediaId, CancellationToken stopToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaId);
        stopToken.ThrowIfCancellationRequested();
        Record(new RecordedWhatsAppRequest(RecordedWhatsAppOperation.DownloadMedia, mediaId: mediaId));
        return Task.FromResult(Dequeue<WhatsAppMediaDownload>(_mediaDownloadResults, "media download"));
    }

    /// <inheritdoc />
    public Task DeleteMediaAsync(string mediaId, CancellationToken stopToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaId);
        stopToken.ThrowIfCancellationRequested();
        Record(new RecordedWhatsAppRequest(RecordedWhatsAppOperation.DeleteMedia, mediaId: mediaId));
        DequeueVoidResult();
        return Task.CompletedTask;
    }

    private void Record(RecordedWhatsAppRequest request)
    {
        lock (_gate)
        {
            _requests.Add(request);
        }
    }

    private SendMessageResponse DequeueMessageResult() => Dequeue<SendMessageResponse>(_messageResults, "send message");

    private void DequeueVoidResult()
    {
        if (_voidResults.TryDequeue(out var item))
        {
            if (item is Exception ex)
            {
                throw ex;
            }
        }
    }

    private static T Dequeue<T>(ConcurrentQueue<object> queue, string operation)
    {
        if (!queue.TryDequeue(out var item))
        {
            if (typeof(T) == typeof(SendMessageResponse))
            {
                return (T)(object)CreateDefaultSendResponse();
            }

            throw new InvalidOperationException($"No queued result for {operation}.");
        }

        if (item is Exception ex)
        {
            throw ex;
        }

        return (T)item;
    }

    /// <summary>Creates a minimal successful send response for tests that did not queue one.</summary>
    public static SendMessageResponse CreateDefaultSendResponse(string messageId = "wamid.test") =>
        new(
            [new WhatsAppMessageId { Id = messageId }],
            [],
            new WhatsAppResponseMetadata(HttpStatusCode.OK, null, null, new Dictionary<string, string>()));
}
