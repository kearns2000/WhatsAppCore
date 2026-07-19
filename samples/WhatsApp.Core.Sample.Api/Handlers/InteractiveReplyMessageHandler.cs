using WhatsApp.Core.AspNetCore.Dispatch;
using WhatsApp.Core.AspNetCore.Webhooks;

namespace WhatsApp.Core.Sample.Api.Handlers;

/// <summary>
/// Logs inbound interactive reply webhooks using safe metadata only.
/// </summary>
public sealed class InteractiveReplyMessageHandler(ILogger<InteractiveReplyMessageHandler> logger)
    : IWhatsAppWebhookHandler<WhatsAppInteractiveReplyMessageEvent>
{
    /// <inheritdoc />
    public Task HandleAsync(WhatsAppInteractiveReplyMessageEvent notification, CancellationToken stopToken)
    {
        logger.LogInformation(
            "Received inbound interactive reply. EventType={EventType}, MessageId={MessageId}, InteractiveType={InteractiveType}",
            nameof(WhatsAppInteractiveReplyMessageEvent),
            notification.MessageId,
            notification.InteractiveType);

        return Task.CompletedTask;
    }
}
