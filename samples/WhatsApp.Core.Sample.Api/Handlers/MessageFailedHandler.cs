using WhatsApp.Core.AspNetCore.Dispatch;
using WhatsApp.Core.AspNetCore.Webhooks;

namespace WhatsApp.Core.Sample.Api.Handlers;

/// <summary>
/// Logs message failed status webhooks using safe metadata only.
/// </summary>
public sealed class MessageFailedHandler(ILogger<MessageFailedHandler> logger)
    : IWhatsAppWebhookHandler<WhatsAppMessageFailedEvent>
{
    /// <inheritdoc />
    public Task HandleAsync(WhatsAppMessageFailedEvent notification, CancellationToken stopToken)
    {
        logger.LogWarning(
            "Message delivery failed. EventType={EventType}, MessageId={MessageId}, ErrorCount={ErrorCount}",
            nameof(WhatsAppMessageFailedEvent),
            notification.MessageId,
            notification.Errors.Count);

        return Task.CompletedTask;
    }
}
