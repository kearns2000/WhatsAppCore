using System.Net.Http.Headers;

namespace WhatsApp.Core.Internal;

/// <summary>
/// Extracts a usable <see cref="TimeSpan"/> delay from an HTTP <c>Retry-After</c> header, which
/// may be expressed either as a number of seconds or as an absolute HTTP date.
/// </summary>
internal static class HttpRetryAfterParser
{

    /// <summary>
    /// Parses the <c>Retry-After</c> header, if present, into a non-negative delay.
    /// </summary>
    /// <param name="headers">The response headers to inspect.</param>
    /// <param name="utcNow">The current UTC time, used to convert absolute dates into a delay.</param>
    /// <returns>The delay to wait before retrying, or <see langword="null"/> if no header was present.</returns>
    public static TimeSpan? Parse(HttpResponseHeaders headers, DateTimeOffset utcNow)
    {
        var retryAfter = headers.RetryAfter;
        if (retryAfter is null)
        {
            return null;
        }

        if (retryAfter.Delta is { } delta)
        {
            return delta < TimeSpan.Zero ? TimeSpan.Zero : delta;
        }

        if (retryAfter.Date is { } date)
        {
            var remaining = date - utcNow;
            return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
        }

        return null;
    }
}
