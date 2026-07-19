namespace WhatsApp.Core.Sample.Api.Models;

/// <summary>
/// Request body for sending a free-form text message.
/// </summary>
public sealed class SendTextMessageRequest
{
    /// <summary>Recipient phone number in E.164 format (with or without a leading <c>+</c>).</summary>
    public required string To { get; init; }

    /// <summary>The text content of the message.</summary>
    public required string Body { get; init; }

    /// <summary>Whether to render a link preview for the first URL in the body.</summary>
    public bool PreviewUrl { get; init; }

    /// <summary>Optional WhatsApp message id (<c>wamid...</c>) of the message being replied to.</summary>
    public string? ReplyToMessageId { get; init; }
}
