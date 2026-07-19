namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// Reports that a message this application sent was accepted by WhatsApp and sent onward to
/// the recipient's device.
/// </summary>
public sealed record WhatsAppMessageSentEvent : WhatsAppMessageStatusEvent;
