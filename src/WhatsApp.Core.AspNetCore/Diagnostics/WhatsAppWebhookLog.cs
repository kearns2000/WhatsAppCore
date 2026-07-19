using Microsoft.Extensions.Logging;

namespace WhatsApp.Core.AspNetCore.Diagnostics;

/// <summary>
/// Source-generated, structured log messages emitted while receiving and dispatching WhatsApp
/// webhooks. Deliberately excludes signatures, app secrets, message content, and phone
/// numbers; only route patterns, event types, and message/WABA ids (never phone numbers) are
/// logged.
/// </summary>
internal static partial class WhatsAppWebhookLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Received webhook delivery on {Route} ({ByteCount} bytes).")]
    public static partial void WebhookReceived(ILogger logger, string route, long byteCount);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Webhook delivery on {Route} rejected: missing or invalid X-Hub-Signature-256 signature.")]
    public static partial void SignatureValidationFailed(ILogger logger, string route);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Webhook delivery on {Route} could not be signature-validated because no app secret is configured for account '{AccountName}'.")]
    public static partial void AppSecretNotConfigured(ILogger logger, string route, string accountName);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Webhook delivery on {Route} rejected: payload exceeded the maximum allowed size of {MaxBytes} bytes.")]
    public static partial void PayloadTooLarge(ILogger logger, string route, long maxBytes);

    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Webhook delivery on {Route} could not be parsed as a valid webhook envelope.")]
    public static partial void WebhookParseFailed(ILogger logger, Exception exception, string route);

    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "Parsed {EventCount} event(s) from webhook delivery on {Route}.")]
    public static partial void WebhookParsed(ILogger logger, string route, int eventCount);

    [LoggerMessage(EventId = 7, Level = LogLevel.Error, Message = "Webhook handler {HandlerType} failed while processing {EventType} (message id: {MessageId}).")]
    public static partial void HandlerFailed(ILogger logger, Exception exception, string handlerType, string eventType, string? messageId);

    [LoggerMessage(EventId = 8, Level = LogLevel.Error, Message = "Webhook deduplicator {DeduplicatorType} failed while processing {EventType} (message id: {MessageId}).")]
    public static partial void DeduplicatorFailed(ILogger logger, Exception exception, string deduplicatorType, string eventType, string? messageId);

    [LoggerMessage(EventId = 9, Level = LogLevel.Warning, Message = "[{AccountName}] RequireSignatureValidation is set to false; inbound webhooks will be processed without verifying that they came from Meta. This must never be enabled in production.")]
    public static partial void SignatureValidationDisabled(ILogger logger, string accountName);

    [LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = "Webhook verification handshake on {Route} succeeded for mode '{Mode}'.")]
    public static partial void VerificationSucceeded(ILogger logger, string route, string mode);

    [LoggerMessage(EventId = 11, Level = LogLevel.Warning, Message = "Webhook verification handshake on {Route} failed for mode '{Mode}'.")]
    public static partial void VerificationFailed(ILogger logger, string route, string? mode);

    [LoggerMessage(EventId = 12, Level = LogLevel.Warning, Message = "Using MemoryWhatsAppWebhookDeduplicator (process-local). Meta redeliveries after process restart, or across multiple instances, are not suppressed. Register a durable IWhatsAppWebhookDeduplicator for production multi-instance deployments.")]
    public static partial void MemoryDeduplicatorProcessLocal(ILogger logger);
}
