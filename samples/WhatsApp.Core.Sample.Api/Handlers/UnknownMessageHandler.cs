using WhatsApp.Core.AspNetCore.Dispatch;
using WhatsApp.Core.AspNetCore.Webhooks;

namespace WhatsApp.Core.Sample.Api.Handlers;

/// <summary>
/// Logs unknown inbound message webhooks using safe metadata only.
/// </summary>
public sealed class UnknownMessageHandler(ILogger<UnknownMessageHandler> logger)
    : IWhatsAppWebhookHandler<UnknownWhatsAppMessageEvent>
{
    /// <inheritdoc />
    public Task HandleAsync(UnknownWhatsAppMessageEvent notification, CancellationToken stopToken)
    {
        logger.LogInformation(
            "Received unknown message type. EventType={EventType}, MessageId={MessageId}, MessageType={MessageType}",
            nameof(UnknownWhatsAppMessageEvent),
            notification.MessageId,
            notification.MessageType);

        return Task.CompletedTask;
    }
}
