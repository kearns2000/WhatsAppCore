using WhatsApp.Core.AspNetCore.Dispatch;
using WhatsApp.Core.AspNetCore.Webhooks;

namespace WhatsApp.Core.Sample.Api.Handlers;

/// <summary>
/// Logs inbound text message webhooks using safe metadata only.
/// </summary>
public sealed class TextMessageHandler(ILogger<TextMessageHandler> logger)
    : IWhatsAppWebhookHandler<WhatsAppTextMessageEvent>
{
    /// <inheritdoc />
    public Task HandleAsync(WhatsAppTextMessageEvent notification, CancellationToken stopToken)
    {
        logger.LogInformation(
            "Received inbound text message. EventType={EventType}, MessageId={MessageId}",
            nameof(WhatsAppTextMessageEvent),
            notification.MessageId);

        return Task.CompletedTask;
    }
}
