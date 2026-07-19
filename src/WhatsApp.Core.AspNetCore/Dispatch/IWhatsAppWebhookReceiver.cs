using WhatsApp.Core.AspNetCore.Options;
using WhatsApp.Core.AspNetCore.Webhooks;

namespace WhatsApp.Core.AspNetCore.Dispatch;

/// <summary>
/// Receives the batch of typed events parsed from a single inbound webhook delivery and
/// dispatches them to registered handlers.
/// </summary>
/// <remarks>
/// The default implementation dispatches to in-process
/// <see cref="IWhatsAppWebhookHandler{TEvent}"/> registrations. Production applications that
/// perform substantial work should acknowledge the webhook quickly and replace this receiver
/// with one that writes events to durable infrastructure (Service Bus, RabbitMQ, Kafka, etc.).
/// This library does not provide an in-memory queue that claims guaranteed delivery.
/// </remarks>
public interface IWhatsAppWebhookReceiver
{
    /// <summary>
    /// Processes the events parsed from one webhook delivery using the dispatch settings from
    /// <paramref name="options"/> (including per-endpoint overrides supplied to
    /// <c>MapWhatsAppWebhook</c>).
    /// </summary>
    /// <param name="events">The typed events. A single delivery may contain many.</param>
    /// <param name="options">The webhook options that applied to this delivery (body limits and
    /// signature settings are already enforced; dispatch mode and parallelism are read here).</param>
    /// <param name="stopToken">A token used to cancel processing.</param>
    Task ReceiveAsync(
        IReadOnlyList<WhatsAppWebhookEvent> events,
        WhatsAppWebhookOptions options,
        CancellationToken stopToken);
}
