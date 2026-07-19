using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace WhatsApp.Core.AspNetCore.Diagnostics;

/// <summary>
/// Provides the <see cref="System.Diagnostics.ActivitySource"/> and
/// <see cref="System.Diagnostics.Metrics.Meter"/> used to instrument webhook receiving,
/// signature validation, parsing, and dispatch, following the same naming and tagging
/// conventions as <see cref="WhatsApp.Core.Diagnostics.WhatsAppDiagnostics"/> in the base
/// package. Tags are always low-cardinality and non-sensitive (event type, dispatch mode -
/// never phone numbers, message content, or signatures).
/// </summary>
public static class WhatsAppWebhookDiagnostics
{
    /// <summary>
    /// The name of the <see cref="System.Diagnostics.ActivitySource"/> used to trace webhook
    /// processing.
    /// </summary>
    public const string ActivitySourceName = "WhatsApp.Core.AspNetCore";

    /// <summary>
    /// The name of the <see cref="System.Diagnostics.Metrics.Meter"/> used to record webhook
    /// metrics.
    /// </summary>
    public const string MeterName = "WhatsApp.Core.AspNetCore";

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> WebhooksReceivedCounter = Meter.CreateCounter<long>(
        "whatsapp.core.aspnetcore.webhooks_received",
        unit: "{webhook}",
        description: "Inbound webhook deliveries received.");

    private static readonly Counter<long> InvalidSignaturesCounter = Meter.CreateCounter<long>(
        "whatsapp.core.aspnetcore.invalid_signatures",
        unit: "{webhook}",
        description: "Inbound webhook deliveries rejected for an invalid or missing signature.");

    private static readonly Counter<long> EventsParsedCounter = Meter.CreateCounter<long>(
        "whatsapp.core.aspnetcore.events_parsed",
        unit: "{event}",
        description: "Typed webhook events successfully parsed from inbound deliveries.");

    private static readonly Counter<long> DispatchFailuresCounter = Meter.CreateCounter<long>(
        "whatsapp.core.aspnetcore.dispatch_failures",
        unit: "{failure}",
        description: "Failures while dispatching a parsed webhook event to a handler or deduplicator.");

    /// <summary>
    /// Starts an activity tracing receipt and processing of one webhook delivery.
    /// </summary>
    /// <param name="route">The route pattern the webhook was received on.</param>
    /// <returns>The started activity, or <see langword="null"/> if no listener is active.</returns>
    internal static Activity? StartReceiveActivity(string route)
    {
        var activity = ActivitySource.StartActivity("WhatsApp.Core.AspNetCore/receive_webhook", ActivityKind.Server);
        activity?.SetTag("whatsapp.webhook.route", route);
        return activity;
    }

    /// <summary>
    /// Starts an activity tracing signature validation of one webhook delivery.
    /// </summary>
    /// <returns>The started activity, or <see langword="null"/> if no listener is active.</returns>
    internal static Activity? StartValidateSignatureActivity() =>
        ActivitySource.StartActivity("WhatsApp.Core.AspNetCore/validate_signature", ActivityKind.Internal);

    /// <summary>
    /// Starts an activity tracing parsing of a webhook payload into typed events.
    /// </summary>
    /// <returns>The started activity, or <see langword="null"/> if no listener is active.</returns>
    internal static Activity? StartParseActivity() =>
        ActivitySource.StartActivity("WhatsApp.Core.AspNetCore/parse_webhook", ActivityKind.Internal);

    /// <summary>
    /// Starts an activity tracing dispatch of a single parsed event to its registered handlers.
    /// </summary>
    /// <param name="eventType">The concrete event type being dispatched (e.g. <c>"WhatsAppTextMessageEvent"</c>).</param>
    /// <returns>The started activity, or <see langword="null"/> if no listener is active.</returns>
    internal static Activity? StartDispatchActivity(string eventType)
    {
        var activity = ActivitySource.StartActivity("WhatsApp.Core.AspNetCore/dispatch_event", ActivityKind.Internal);
        activity?.SetTag("whatsapp.webhook.event_type", eventType);
        return activity;
    }

    /// <summary>
    /// Records that a webhook delivery was received.
    /// </summary>
    /// <param name="route">The route pattern the webhook was received on.</param>
    internal static void RecordWebhookReceived(string route) =>
        WebhooksReceivedCounter.Add(1, new KeyValuePair<string, object?>("whatsapp.webhook.route", route));

    /// <summary>
    /// Records that a webhook delivery was rejected for an invalid or missing signature.
    /// </summary>
    /// <param name="route">The route pattern the webhook was received on.</param>
    internal static void RecordInvalidSignature(string route) =>
        InvalidSignaturesCounter.Add(1, new KeyValuePair<string, object?>("whatsapp.webhook.route", route));

    /// <summary>
    /// Records that a batch of events was successfully parsed from a webhook delivery.
    /// </summary>
    /// <param name="count">The number of events parsed.</param>
    internal static void RecordEventsParsed(int count)
    {
        if (count > 0)
        {
            EventsParsedCounter.Add(count);
        }
    }

    /// <summary>
    /// Records a dispatch failure (deduplicator or handler threw).
    /// </summary>
    /// <param name="eventType">The concrete event type being dispatched.</param>
    internal static void RecordDispatchFailure(string eventType) =>
        DispatchFailuresCounter.Add(1, new KeyValuePair<string, object?>("whatsapp.webhook.event_type", eventType));
}
