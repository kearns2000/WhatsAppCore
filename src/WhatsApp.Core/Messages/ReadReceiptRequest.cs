using System.Text.Json.Nodes;
using WhatsApp.Core.Errors;

namespace WhatsApp.Core.Messages;

/// <summary>
/// A read receipt, marking a previously received message as read. Deliberately separate from
/// <see cref="WhatsAppMessageRequest"/> because read receipts have no recipient (<c>to</c>)
/// field; they are addressed implicitly by <see cref="MessageId"/>.
/// </summary>
internal sealed record ReadReceiptRequest
{
    /// <summary>
    /// Gets the WhatsApp message id (<c>wamid...</c>) of the message to mark as read.
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    /// Builds the JSON payload to send to the Graph API <c>/messages</c> endpoint.
    /// </summary>
    /// <exception cref="WhatsAppValidationException">The request is invalid.</exception>
    public JsonObject ToJsonPayload()
    {
        if (string.IsNullOrWhiteSpace(MessageId))
        {
            throw new WhatsAppValidationException("A read receipt must specify the id of the message being marked as read.");
        }

        return new JsonObject
        {
            ["messaging_product"] = "whatsapp",
            ["status"] = "read",
            ["message_id"] = MessageId,
        };
    }
}
