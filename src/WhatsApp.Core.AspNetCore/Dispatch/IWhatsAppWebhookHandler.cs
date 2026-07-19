using WhatsApp.Core.AspNetCore.Webhooks;

namespace WhatsApp.Core.AspNetCore.Dispatch;

/// <summary>
/// Handles a single strongly-typed webhook event. Register implementations with
/// <c>AddWhatsAppWebhookHandler</c>; the default <see cref="IWhatsAppWebhookReceiver"/>
/// resolves every registered handler for an event's concrete runtime type (allowing multiple
/// independent handlers per event type) and invokes them in registration order.
/// </summary>
/// <typeparam name="TEvent">The concrete <see cref="WhatsAppWebhookEvent"/> subtype this handler processes.</typeparam>
public interface IWhatsAppWebhookHandler<in TEvent>
    where TEvent : WhatsAppWebhookEvent
{
    /// <summary>
    /// Handles <paramref name="notification"/>.
    /// </summary>
    /// <param name="notification">The event to handle.</param>
    /// <param name="stopToken">A token that is canceled if the underlying HTTP request is aborted.</param>
    /// <returns>A task that completes once handling has finished.</returns>
    /// <remarks>
    /// Exceptions thrown here are caught by the default <see cref="IWhatsAppWebhookReceiver"/>,
    /// logged (with the event type and safe ids only - never message content or phone numbers),
    /// and do not prevent other handlers or other events in the same delivery from running.
    /// </remarks>
    Task HandleAsync(TEvent notification, CancellationToken stopToken);
}
