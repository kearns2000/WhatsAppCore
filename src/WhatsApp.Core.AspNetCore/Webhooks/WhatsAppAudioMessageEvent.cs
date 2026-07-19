namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// An inbound audio (including voice note) message.
/// </summary>
public sealed record WhatsAppAudioMessageEvent : WhatsAppMessageEvent
{
    /// <summary>
    /// Gets the audio media attached to this message.
    /// </summary>
    public required WhatsAppInboundMedia Audio { get; init; }

    /// <summary>
    /// Gets a value indicating whether this audio message is a voice note recorded directly in
    /// WhatsApp, as opposed to an audio file shared from elsewhere.
    /// </summary>
    public bool IsVoiceNote { get; init; }
}
