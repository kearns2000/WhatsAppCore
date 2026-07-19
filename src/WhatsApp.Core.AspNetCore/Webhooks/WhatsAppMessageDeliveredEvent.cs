namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// Reports that a message this application sent was delivered to the recipient's device.
/// </summary>
public sealed record WhatsAppMessageDeliveredEvent : WhatsAppMessageStatusEvent;
