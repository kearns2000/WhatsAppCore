using System.Collections.Frozen;
using System.Net;

namespace WhatsApp.Core.Responses;

/// <summary>
/// HTTP-level metadata captured alongside a successful Graph API response, useful for logging,
/// diagnostics, and honoring rate-limit hints without needing to inspect raw HTTP headers.
/// </summary>
public sealed class WhatsAppResponseMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppResponseMetadata"/> class.
    /// </summary>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    /// <param name="requestId">Meta's trace id for the request, if present.</param>
    /// <param name="retryAfter">The delay suggested by a <c>Retry-After</c> header, if present.</param>
    /// <param name="headers">The response headers, keyed by header name.</param>
    public WhatsAppResponseMetadata(
        HttpStatusCode statusCode,
        string? requestId,
        TimeSpan? retryAfter,
        IReadOnlyDictionary<string, string> headers)
    {
        StatusCode = statusCode;
        RequestId = requestId;
        RetryAfter = retryAfter;
        Headers = headers.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the HTTP status code returned by the Graph API for this request.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Gets Meta's trace id for the request (from the <c>x-fb-trace-id</c> header), if present.
    /// </summary>
    public string? RequestId { get; }

    /// <summary>
    /// Gets the delay Meta suggests waiting before making another request, derived from the
    /// <c>Retry-After</c> response header, if present.
    /// </summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>
    /// Gets the response headers, keyed by header name using a case-insensitive comparer.
    /// Values for multi-valued headers are comma-joined.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; }
}
