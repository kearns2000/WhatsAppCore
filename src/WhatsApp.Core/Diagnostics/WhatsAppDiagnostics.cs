using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;

namespace WhatsApp.Core.Diagnostics;

/// <summary>
/// Provides the <see cref="System.Diagnostics.ActivitySource"/> and
/// <see cref="System.Diagnostics.Metrics.Meter"/> used to instrument this library, along with
/// helpers for recording activities and metrics using only low-cardinality, non-sensitive tags
/// (account name, operation, message type, status/error codes - never phone numbers, message
/// content, media URLs, or tokens).
/// </summary>
public static class WhatsAppDiagnostics
{
    /// <summary>
    /// The name of the <see cref="System.Diagnostics.ActivitySource"/> used to trace WhatsApp
    /// Cloud API operations (send, upload, get, download, delete).
    /// </summary>
    public const string ActivitySourceName = "WhatsApp.Core";

    /// <summary>
    /// The name of the <see cref="System.Diagnostics.Metrics.Meter"/> used to record WhatsApp
    /// Cloud API metrics.
    /// </summary>
    public const string MeterName = "WhatsApp.Core";

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> RequestsCounter =
        Meter.CreateCounter<long>("whatsapp.core.requests", unit: "{request}", description: "Outbound Graph API requests.");

    private static readonly Counter<long> FailuresCounter =
        Meter.CreateCounter<long>("whatsapp.core.request_failures", unit: "{request}", description: "Failed outbound Graph API requests.");

    private static readonly Histogram<double> DurationHistogram =
        Meter.CreateHistogram<double>("whatsapp.core.request.duration", unit: "ms", description: "Outbound Graph API request duration.");

    private static readonly Histogram<long> MediaBytesHistogram =
        Meter.CreateHistogram<long>("whatsapp.core.media.bytes", unit: "By", description: "Size of media uploaded or downloaded.");

    /// <summary>
    /// Starts an activity for a client operation, tagged with safe, low-cardinality metadata.
    /// </summary>
    /// <param name="operation">The operation name (e.g. <c>"send_message"</c>, <c>"upload_media"</c>).</param>
    /// <param name="accountName">The logical account name performing the operation.</param>
    /// <param name="messageType">The message type, if applicable (e.g. <c>"text"</c>).</param>
    /// <returns>The started activity, or <see langword="null"/> if no listener is active.</returns>
    internal static Activity? StartActivity(string operation, string accountName, string? messageType = null)
    {
        var activity = ActivitySource.StartActivity($"WhatsApp.Core/{operation}", ActivityKind.Client);
        if (activity is null)
        {
            return null;
        }

        activity.SetTag("whatsapp.account", accountName);
        activity.SetTag("whatsapp.operation", operation);
        if (messageType is not null)
        {
            activity.SetTag("whatsapp.message_type", messageType);
        }

        return activity;
    }

    /// <summary>
    /// Records that an outbound request was attempted.
    /// </summary>
    /// <param name="accountName">The logical account name.</param>
    /// <param name="operation">The operation name.</param>
    internal static void RecordRequest(string accountName, string operation) =>
        RequestsCounter.Add(1, new KeyValuePair<string, object?>("whatsapp.account", accountName), new KeyValuePair<string, object?>("whatsapp.operation", operation));

    /// <summary>
    /// Records that an outbound request failed.
    /// </summary>
    /// <param name="accountName">The logical account name.</param>
    /// <param name="operation">The operation name.</param>
    /// <param name="statusCode">The HTTP status code, if the failure came from a Graph API response.</param>
    /// <param name="errorCode">The Graph API error code, if known.</param>
    internal static void RecordFailure(string accountName, string operation, HttpStatusCode? statusCode, int? errorCode)
    {
        var tags = new TagList
        {
            { "whatsapp.account", accountName },
            { "whatsapp.operation", operation },
        };

        if (statusCode is not null)
        {
            tags.Add("whatsapp.status_code", (int)statusCode.Value);
        }

        if (errorCode is not null)
        {
            tags.Add("whatsapp.error_code", errorCode.Value);
        }

        FailuresCounter.Add(1, tags);
    }

    /// <summary>
    /// Records the duration of a completed (successful or failed) outbound request.
    /// </summary>
    /// <param name="accountName">The logical account name.</param>
    /// <param name="operation">The operation name.</param>
    /// <param name="elapsedMilliseconds">The elapsed duration, in milliseconds.</param>
    internal static void RecordDuration(string accountName, string operation, double elapsedMilliseconds) =>
        DurationHistogram.Record(
            elapsedMilliseconds,
            new KeyValuePair<string, object?>("whatsapp.account", accountName),
            new KeyValuePair<string, object?>("whatsapp.operation", operation));

    /// <summary>
    /// Records the size of media uploaded or downloaded.
    /// </summary>
    /// <param name="accountName">The logical account name.</param>
    /// <param name="operation">The operation name (e.g. <c>"upload_media"</c>, <c>"download_media"</c>).</param>
    /// <param name="bytes">The number of bytes transferred.</param>
    internal static void RecordMediaBytes(string accountName, string operation, long bytes) =>
        MediaBytesHistogram.Record(
            bytes,
            new KeyValuePair<string, object?>("whatsapp.account", accountName),
            new KeyValuePair<string, object?>("whatsapp.operation", operation));
}
