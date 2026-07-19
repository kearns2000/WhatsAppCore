namespace WhatsApp.Core.Responses;

/// <summary>
/// The result of successfully sending a message (or marking one as read) through the WhatsApp
/// Cloud API.
/// </summary>
public sealed class SendMessageResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SendMessageResponse"/> class.
    /// </summary>
    /// <param name="messages">The ids of the messages that were accepted.</param>
    /// <param name="contacts">The recipient contacts resolved by the API.</param>
    /// <param name="metadata">HTTP-level metadata captured from the response.</param>
    public SendMessageResponse(
        IReadOnlyList<WhatsAppMessageId> messages,
        IReadOnlyList<WhatsAppResponseContact> contacts,
        WhatsAppResponseMetadata metadata)
    {
        Messages = messages;
        Contacts = contacts;
        Metadata = metadata;
    }

    /// <summary>
    /// Gets the ids of the messages that were accepted by the Graph API. A single send call
    /// typically results in exactly one entry.
    /// </summary>
    public IReadOnlyList<WhatsAppMessageId> Messages { get; }

    /// <summary>
    /// Gets the recipient contacts resolved by the Graph API for this request.
    /// </summary>
    public IReadOnlyList<WhatsAppResponseContact> Contacts { get; }

    /// <summary>
    /// Gets HTTP-level metadata captured alongside this response.
    /// </summary>
    public WhatsAppResponseMetadata Metadata { get; }
}
