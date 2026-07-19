namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// Reports that the recipient has read a message this application sent.
/// </summary>
public sealed record WhatsAppMessageReadEvent : WhatsAppMessageStatusEvent;
