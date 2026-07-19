namespace WhatsApp.Core.Sample.Api.Models;

/// <summary>
/// Request body for sending an image message.
/// </summary>
public sealed class SendImageMessageRequest
{
    /// <summary>Recipient phone number in E.164 format.</summary>
    public required string To { get; init; }

    /// <summary>Previously uploaded media id. Exactly one of this or <see cref="Link"/> must be set.</summary>
    public string? MediaId { get; init; }

    /// <summary>Publicly reachable image URL. Exactly one of this or <see cref="MediaId"/> must be set.</summary>
    public string? Link { get; init; }

    /// <summary>Optional caption displayed alongside the image.</summary>
    public string? Caption { get; init; }

    /// <summary>Optional WhatsApp message id of the message being replied to.</summary>
    public string? ReplyToMessageId { get; init; }
}
