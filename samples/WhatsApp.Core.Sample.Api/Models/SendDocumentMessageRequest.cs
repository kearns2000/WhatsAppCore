namespace WhatsApp.Core.Sample.Api.Models;

/// <summary>
/// Request body for sending a document message.
/// </summary>
public sealed class SendDocumentMessageRequest
{
    /// <summary>Recipient phone number in E.164 format.</summary>
    public required string To { get; init; }

    /// <summary>Previously uploaded media id. Exactly one of this or <see cref="Link"/> must be set.</summary>
    public string? MediaId { get; init; }

    /// <summary>Publicly reachable document URL. Exactly one of this or <see cref="MediaId"/> must be set.</summary>
    public string? Link { get; init; }

    /// <summary>Optional caption displayed alongside the document.</summary>
    public string? Caption { get; init; }

    /// <summary>Optional file name suggested to the recipient.</summary>
    public string? FileName { get; init; }

    /// <summary>Optional WhatsApp message id of the message being replied to.</summary>
    public string? ReplyToMessageId { get; init; }
}
