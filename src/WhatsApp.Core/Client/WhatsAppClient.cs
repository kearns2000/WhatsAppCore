using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsApp.Core.Configuration;
using WhatsApp.Core.Diagnostics;
using WhatsApp.Core.Errors;
using WhatsApp.Core.Internal;
using WhatsApp.Core.Media;
using WhatsApp.Core.Messages;
using WhatsApp.Core.Responses;
using WhatsApp.Core.Serialization;
using WhatsApp.Core.Serialization.Wire;

namespace WhatsApp.Core.Client;

/// <summary>
/// Default <see cref="IWhatsAppClient"/> implementation, backed by <see cref="IHttpClientFactory"/>.
/// </summary>
internal sealed class WhatsAppClient(
    string accountName,
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<WhatsAppOptions> optionsMonitor,
    ILogger<WhatsAppClient> logger,
    TimeProvider timeProvider) : IWhatsAppClient
{
    /// <inheritdoc />
    public string AccountName { get; } = accountName;

    private WhatsAppOptions Options => optionsMonitor.Get(AccountName);

    /// <inheritdoc />
    public Task<SendMessageResponse> SendMessageAsync(WhatsAppMessageRequest request, CancellationToken stopToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var payload = request.ToJsonPayload();
        return ExecuteAsync(
            "send_message",
            request.Type,
            (httpClient, options, token) => PostMessageAsync(httpClient, options, payload, token),
            stopToken);
    }

    /// <inheritdoc />
    public Task<SendMessageResponse> SendRawMessageAsync(JsonObject payload, CancellationToken stopToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var normalized = NormalizeRawPayload(payload);
        var messageType = normalized.TryGetPropertyValue("type", out var typeNode) && typeNode is JsonValue typeValue
            ? typeValue.GetValue<string>()
            : null;

        return ExecuteAsync(
            "send_message",
            messageType,
            (httpClient, options, token) => PostMessageAsync(httpClient, options, normalized, token),
            stopToken);
    }

    /// <inheritdoc />
    public Task MarkMessageAsReadAsync(string messageId, CancellationToken stopToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        var payload = new ReadReceiptRequest { MessageId = messageId }.ToJsonPayload();
        return ExecuteAsync(
            "mark_as_read",
            null,
            async (httpClient, options, token) =>
            {
                var uri = BuildMessagesUri(options);
                using var content = CreateJsonContent(payload);
                using var response = await httpClient.PostAsync(uri, content, token).ConfigureAwait(false);
                await EnsureSuccessAsync(response, token).ConfigureAwait(false);
            },
            stopToken);
    }

    /// <inheritdoc />
    public Task<MediaUploadResponse> UploadMediaAsync(Stream content, string fileName, string contentType, CancellationToken stopToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        return ExecuteAsync(
            "upload_media",
            null,
            (httpClient, options, token) => UploadMediaCoreAsync(httpClient, options, content, fileName, contentType, token),
            stopToken);
    }

    /// <inheritdoc />
    public Task<MediaMetadata> GetMediaAsync(string mediaId, CancellationToken stopToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaId);
        return ExecuteAsync(
            "get_media",
            null,
            (httpClient, options, token) => GetMediaCoreAsync(httpClient, options, mediaId, token),
            stopToken);
    }

    /// <inheritdoc />
    public async Task<WhatsAppMediaDownload> DownloadMediaAsync(string mediaId, CancellationToken stopToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaId);

        // Always fetch fresh metadata immediately before downloading; the media URL is
        // short-lived and must not be cached across calls.
        var metadata = await GetMediaAsync(mediaId, stopToken).ConfigureAwait(false);

        return await ExecuteAsync(
            "download_media",
            null,
            (httpClient, options, token) => DownloadMediaCoreAsync(httpClient, options, metadata, token),
            stopToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task DeleteMediaAsync(string mediaId, CancellationToken stopToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaId);
        return ExecuteAsync(
            "delete_media",
            null,
            (httpClient, options, token) => DeleteMediaCoreAsync(httpClient, options, mediaId, token),
            stopToken);
    }

    private static async Task<SendMessageResponse> PostMessageAsync(
        HttpClient httpClient, WhatsAppOptions options, JsonObject payload, CancellationToken cancellationToken)
    {
        var uri = BuildMessagesUri(options);
        using var content = CreateJsonContent(payload);
        using var response = await httpClient.PostAsync(uri, content, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        var wire = await ReadJsonAsync<SendMessageWireResponse>(response, cancellationToken).ConfigureAwait(false);
        return new SendMessageResponse(
            wire.Messages ?? [],
            wire.Contacts ?? [],
            BuildMetadata(response));
    }

    private async Task<MediaUploadResponse> UploadMediaCoreAsync(
        HttpClient httpClient,
        WhatsAppOptions options,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        var uri = BuildUri(options, options.PhoneNumberId, "media");

        using var form = new MultipartFormDataContent();
        using var messagingProductContent = new StringContent("whatsapp");
        form.Add(messagingProductContent, "messaging_product");
        using var typeContent = new StringContent(contentType);
        form.Add(typeContent, "type");

        // LeaveOpenStream keeps ownership with the caller: disposing MultipartFormDataContent
        // would otherwise dispose StreamContent and, in turn, the caller's stream.
        using var streamContent = new StreamContent(new LeaveOpenStream(content));
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(streamContent, "file", fileName);

        using var response = await httpClient.PostAsync(uri, form, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        var wire = await ReadJsonAsync<MediaUploadWireResponse>(response, cancellationToken).ConfigureAwait(false);

        if (content.CanSeek)
        {
            WhatsAppDiagnostics.RecordMediaBytes(AccountName, "upload_media", content.Length);
        }

        return new MediaUploadResponse(wire.Id, BuildMetadata(response));
    }

    private async Task<MediaMetadata> GetMediaCoreAsync(
        HttpClient httpClient, WhatsAppOptions options, string mediaId, CancellationToken cancellationToken)
    {
        var uri = BuildMediaMetadataUri(options, mediaId);
        using var response = await SendWithOptionalRetryAsync(
            httpClient, options, "get_media", () => new HttpRequestMessage(HttpMethod.Get, uri), cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        var wire = await ReadJsonAsync<MediaMetadataWireResponse>(response, cancellationToken).ConfigureAwait(false);
        return new MediaMetadata(wire.Id, wire.Url, wire.MimeType, wire.Sha256, wire.FileSize, BuildMetadata(response));
    }

    private async Task<WhatsAppMediaDownload> DownloadMediaCoreAsync(
        HttpClient httpClient, WhatsAppOptions options, MediaMetadata metadata, CancellationToken cancellationToken)
    {
        MediaDownloadUrlValidator.EnsureAllowed(metadata.Url, options);

        var response = await httpClient
            .GetAsync(metadata.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? metadata.MimeType;
            var contentLength = response.Content.Headers.ContentLength ?? metadata.FileSizeBytes;
            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName;

            if (contentLength is { } knownLength)
            {
                WhatsAppDiagnostics.RecordMediaBytes(AccountName, "download_media", knownLength);
            }

            return new WhatsAppMediaDownload(response, stream, contentType, contentLength, fileName);
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    private async Task DeleteMediaCoreAsync(
        HttpClient httpClient, WhatsAppOptions options, string mediaId, CancellationToken cancellationToken)
    {
        var uri = BuildMediaMetadataUri(options, mediaId);
        using var response = await SendWithOptionalRetryAsync(
            httpClient, options, "delete_media", () => new HttpRequestMessage(HttpMethod.Delete, uri), cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendWithOptionalRetryAsync(
        HttpClient httpClient,
        WhatsAppOptions options,
        string operation,
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        var maxAttempts = options.Resilience.EnableSafeRetries
            ? Math.Max(1, options.Resilience.MaxSafeRetries + 1)
            : 1;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var request = requestFactory();
            HttpResponseMessage response;
            try
            {
                response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException) when (attempt < maxAttempts)
            {
                await DelayBeforeRetryAsync(AccountName, operation, attempt, cancellationToken).ConfigureAwait(false);
                continue;
            }

            if (attempt < maxAttempts && IsRetryableStatusCode(response.StatusCode))
            {
                response.Dispose();
                await DelayBeforeRetryAsync(AccountName, operation, attempt, cancellationToken).ConfigureAwait(false);
                continue;
            }

            return response;
        }

        // Unreachable: the loop above always returns or retries until the final attempt, which
        // always returns.
        throw new InvalidOperationException("Unreachable.");
    }

    private Task DelayBeforeRetryAsync(string account, string operation, int attempt, CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1));
        WhatsAppLog.RetryingAfterTransientFailure(logger, account, operation, attempt, delay.TotalMilliseconds);
        return Task.Delay(delay, timeProvider, cancellationToken);
    }

    private static bool IsRetryableStatusCode(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;

    private Task ExecuteAsync(
        string operation,
        string? messageType,
        Func<HttpClient, WhatsAppOptions, CancellationToken, Task> action,
        CancellationToken cancellationToken) =>
        ExecuteAsync<object?>(
            operation,
            messageType,
            async (httpClient, options, token) =>
            {
                await action(httpClient, options, token).ConfigureAwait(false);
                return null;
            },
            cancellationToken);

    private async Task<T> ExecuteAsync<T>(
        string operation,
        string? messageType,
        Func<HttpClient, WhatsAppOptions, CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        var options = Options;
        using var activity = WhatsAppDiagnostics.StartActivity(operation, AccountName, messageType);
        var httpClient = httpClientFactory.CreateClient(WhatsAppHttpClientNames.For(AccountName));
        var startTimestamp = timeProvider.GetTimestamp();

        WhatsAppLog.OperationStarting(logger, AccountName, operation);
        WhatsAppDiagnostics.RecordRequest(AccountName, operation);

        try
        {
            var result = await action(httpClient, options, cancellationToken).ConfigureAwait(false);
            var elapsed = timeProvider.GetElapsedTime(startTimestamp);
            WhatsAppLog.OperationSucceeded(logger, AccountName, operation, elapsed.TotalMilliseconds);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (WhatsAppApiException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.ErrorType);
            activity?.SetTag("whatsapp.status_code", (int)ex.StatusCode);
            if (ex.ErrorCode is { } errorCode)
            {
                activity?.SetTag("whatsapp.error_code", errorCode);
            }

            WhatsAppDiagnostics.RecordFailure(AccountName, operation, ex.StatusCode, ex.ErrorCode);
            WhatsAppLog.ApiOperationFailed(logger, ex, AccountName, operation, (int)ex.StatusCode, ex.ErrorCode);
            throw;
        }
        catch (OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "canceled");
            WhatsAppLog.OperationCanceled(logger, AccountName, operation);
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            WhatsAppDiagnostics.RecordFailure(AccountName, operation, null, null);
            WhatsAppLog.OperationFailed(logger, ex, AccountName, operation);
            throw;
        }
        finally
        {
            WhatsAppDiagnostics.RecordDuration(AccountName, operation, timeProvider.GetElapsedTime(startTimestamp).TotalMilliseconds);
        }
    }

    private static JsonObject NormalizeRawPayload(JsonObject payload)
    {
        var clone = (JsonObject)payload.DeepClone();

        if (clone.TryGetPropertyValue("messaging_product", out var messagingProduct))
        {
            if (messagingProduct is not JsonValue value
                || !value.TryGetValue(out string? stringValue)
                || !string.Equals(stringValue, "whatsapp", StringComparison.Ordinal))
            {
                throw new WhatsAppValidationException("The 'messaging_product' field, when specified, must equal 'whatsapp'.");
            }
        }
        else
        {
            clone["messaging_product"] = "whatsapp";
        }

        if (!clone.TryGetPropertyValue("to", out var to)
            || to is not JsonValue toValue
            || !toValue.TryGetValue(out string? toString)
            || string.IsNullOrWhiteSpace(toString))
        {
            throw new WhatsAppValidationException("The raw message payload must include a non-empty 'to' field.");
        }

        // Note: the destination host/URI is always derived from the client's configured
        // WhatsAppOptions, never from the payload, so nothing in the raw payload can redirect
        // the request elsewhere.
        return clone;
    }

    private static HttpContent CreateJsonContent(JsonObject payload)
    {
        var json = payload.ToJsonString();
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw await WhatsAppErrorParser.CreateExceptionAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
        where T : class
    {
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var result = await System.Text.Json.JsonSerializer
            .DeserializeAsync<T>(stream, WhatsAppJsonSerializerOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        return result ?? throw new WhatsAppApiException(
            $"The WhatsApp Cloud API returned an empty response body where a {typeof(T).Name} was expected.",
            response.StatusCode);
    }

    private static WhatsAppResponseMetadata BuildMetadata(HttpResponseMessage response)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in response.Headers)
        {
            headers[header.Key] = string.Join(",", header.Value);
        }

        foreach (var header in response.Content.Headers)
        {
            headers[header.Key] = string.Join(",", header.Value);
        }

        var requestId = response.Headers.TryGetValues("x-fb-trace-id", out var traceValues)
            ? traceValues.FirstOrDefault()
            : null;
        var retryAfter = HttpRetryAfterParser.Parse(response.Headers, DateTimeOffset.UtcNow);

        return new WhatsAppResponseMetadata(response.StatusCode, requestId, retryAfter, headers);
    }

    private static Uri BuildMessagesUri(WhatsAppOptions options) => BuildUri(options, options.PhoneNumberId, "messages");

    private static Uri BuildMediaMetadataUri(WhatsAppOptions options, string mediaId) =>
        BuildUri(options, [mediaId], queryString: $"phone_number_id={Uri.EscapeDataString(options.PhoneNumberId)}");

    private static Uri BuildUri(WhatsAppOptions options, params string[] segments) => BuildUri(options, segments, queryString: null);

    private static Uri BuildUri(WhatsAppOptions options, IEnumerable<string> segments, string? queryString)
    {
        var baseAddress = options.BaseAddress.ToString().TrimEnd('/');
        var path = string.Join('/', segments.Select(Uri.EscapeDataString));
        var uri = $"{baseAddress}/{options.GraphApiVersion}/{path}";
        if (!string.IsNullOrEmpty(queryString))
        {
            uri += $"?{queryString}";
        }

        return new Uri(uri);
    }
}
