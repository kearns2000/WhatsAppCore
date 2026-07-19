using WhatsApp.Core.AspNetCore.Dispatch;
using WhatsApp.Core.AspNetCore.Webhooks;

namespace WhatsApp.Core.Sample.Api.Handlers;

/// <summary>
/// Logs message delivered status webhooks using safe metadata only.
/// </summary>
public sealed class MessageDeliveredHandler(ILogger<MessageDeliveredHandler> logger)
    : IWhatsAppWebhookHandler<WhatsAppMessageDeliveredEvent>
{
    /// <inheritdoc />
    public Task HandleAsync(WhatsAppMessageDeliveredEvent notification, CancellationToken stopToken)
    {
        logger.LogInformation(
            "Message delivered. EventType={EventType}, MessageId={MessageId}",
            nameof(WhatsAppMessageDeliveredEvent),
            notification.MessageId);

        return Task.CompletedTask;
    }
}
