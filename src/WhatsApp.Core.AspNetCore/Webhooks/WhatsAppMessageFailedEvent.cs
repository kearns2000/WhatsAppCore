namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// Reports that a message this application sent could not be delivered.
/// </summary>
public sealed record WhatsAppMessageFailedEvent : WhatsAppMessageStatusEvent
{
    /// <summary>
    /// Gets the errors Meta reported for this delivery failure. Always contains at least one
    /// entry.
    /// </summary>
    public required IReadOnlyList<WhatsAppWebhookError> Errors { get; init; }
}
