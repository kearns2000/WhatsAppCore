using System.Net;
using System.Text.Json;

namespace WhatsApp.Core.Errors;

/// <summary>
/// Thrown when the Meta Graph API returns a non-success HTTP response for a WhatsApp Cloud API
/// request. Carries the parsed Graph API error details in addition to the HTTP status code, so
/// that callers can make informed retry and alerting decisions without inspecting raw JSON.
/// </summary>
/// <remarks>
/// Instances never include the access token, app secret, or verify token used to make the
/// request. <see cref="Exception.Message"/> and <see cref="ErrorData"/> are derived solely from
/// the response body returned by Meta.
/// </remarks>
public sealed class WhatsAppApiException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppApiException"/> class.
    /// </summary>
    /// <param name="message">A human-readable description of the failure.</param>
    /// <param name="statusCode">The HTTP status code returned by the Graph API.</param>
    /// <param name="errorCode">The top-level Graph API error code, if the response body could be parsed.</param>
    /// <param name="errorSubcode">The Graph API error subcode, if the response body could be parsed.</param>
    /// <param name="errorType">The Graph API error type, if the response body could be parsed.</param>
    /// <param name="metaTraceId">Meta's trace id for the request, useful when contacting support.</param>
    /// <param name="isTransient">Whether the error is likely to succeed if retried after a delay.</param>
    /// <param name="retryAfter">The delay Meta suggests waiting before retrying, if provided.</param>
    /// <param name="errorData">Additional structured error data returned by the Graph API, if any.</param>
    /// <param name="innerException">The underlying exception, if the failure originated from a transport-level error.</param>
    public WhatsAppApiException(
        string message,
        HttpStatusCode statusCode,
        int? errorCode = null,
        int? errorSubcode = null,
        string? errorType = null,
        string? metaTraceId = null,
        bool? isTransient = null,
        TimeSpan? retryAfter = null,
        JsonElement? errorData = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        ErrorSubcode = errorSubcode;
        ErrorType = errorType;
        MetaTraceId = metaTraceId;
        IsTransient = isTransient;
        RetryAfter = retryAfter;
        ErrorData = errorData;
    }

    /// <summary>
    /// Gets the HTTP status code returned by the Graph API.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Gets the top-level Graph API error code (the <c>error.code</c> field), if the response
    /// body could be parsed as a structured Graph API error.
    /// </summary>
    public int? ErrorCode { get; }

    /// <summary>
    /// Gets the Graph API error subcode (the <c>error.error_subcode</c> field), if present.
    /// </summary>
    public int? ErrorSubcode { get; }

    /// <summary>
    /// Gets the Graph API error type (the <c>error.type</c> field), if present.
    /// </summary>
    public string? ErrorType { get; }

    /// <summary>
    /// Gets Meta's trace id for the failed request (the <c>error.fbtrace_id</c> field), which is
    /// useful when reporting issues to Meta support.
    /// </summary>
    public string? MetaTraceId { get; }

    /// <summary>
    /// Gets a value indicating whether this error is likely transient and may succeed if
    /// retried after a delay. <see langword="null"/> when the transience of the error could not
    /// be determined.
    /// </summary>
    public bool? IsTransient { get; }

    /// <summary>
    /// Gets the delay that Meta suggests waiting before retrying the request, derived from the
    /// <c>Retry-After</c> response header, if present.
    /// </summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>
    /// Gets additional structured error data returned by the Graph API (the <c>error.error_data</c>
    /// field), such as further details for messaging policy violations.
    /// </summary>
    public JsonElement? ErrorData { get; }
}
