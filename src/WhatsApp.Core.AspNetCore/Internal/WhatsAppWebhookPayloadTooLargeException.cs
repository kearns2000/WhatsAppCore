namespace WhatsApp.Core.AspNetCore.Internal;

/// <summary>
/// Signals that an inbound webhook request body exceeded the configured maximum size while it
/// was being read. Caught internally by the endpoint handler and translated into a
/// <c>413 Payload Too Large</c> response; never escapes this assembly.
/// </summary>
internal sealed class WhatsAppWebhookPayloadTooLargeException : Exception;
