using System.Net;

namespace WhatsApp.Core.Internal;

/// <summary>
/// Classifies Graph API errors as transient (safe to retry after a delay) or permanent, based
/// on the HTTP status code and, where available, the Graph API error code and subcode.
/// </summary>
internal static class GraphErrorClassifier
{
    /// <summary>
    /// Determines whether a failed request is likely to succeed if retried after a delay.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned by the Graph API.</param>
    /// <param name="errorCode">The Graph API top-level error code, if known.</param>
    /// <param name="errorSubcode">The Graph API error subcode, if known.</param>
    /// <returns><see langword="true"/> if the error is considered transient; otherwise <see langword="false"/>.</returns>
    public static bool IsTransient(HttpStatusCode statusCode, int? errorCode, int? errorSubcode)
    {
        if (IsTransientStatusCode(statusCode))
        {
            return true;
        }

        // Well-known Graph API error codes that represent throttling or transient upstream
        // instability rather than a permanent client-side mistake.
        return errorCode switch
        {
            1 => true, // An unknown error occurred.
            2 => true, // Service temporarily unavailable.
            4 => true, // Application request limit reached.
            80007 => true, // WhatsApp Business Account rate limit reached.
            130429 => true, // Rate limit hit for sending messages.
            131048 => true, // Spam rate limit hit.
            131056 => true, // Pair rate limit hit (too many messages to the same recipient).
            _ => false,
        };
    }

    private static bool IsTransientStatusCode(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
}
