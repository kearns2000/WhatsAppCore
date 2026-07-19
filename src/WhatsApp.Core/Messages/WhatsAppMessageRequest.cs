using System.Text.Json.Nodes;
using WhatsApp.Core.Internal;

namespace WhatsApp.Core.Messages;

/// <summary>
/// Base type for every outbound WhatsApp message request (text, template, media, location,
/// contacts, interactive, and reaction messages). Every concrete message type produces a JSON
/// payload of the shape:
/// <c>{ "messaging_product": "whatsapp", "recipient_type": "individual", "to": "...", "type": "...", "&lt;type&gt;": { ... } }</c>.
/// </summary>
/// <remarks>
/// Instances are immutable records. The recipient in <see cref="To"/> is validated and
/// normalized (E.164, digits only, optional leading '+') when the request is serialized; it is
/// not normalized eagerly on construction so that validation errors are consistently surfaced
/// at send time via <see cref="Errors.WhatsAppValidationException"/>.
/// </remarks>
public abstract record WhatsAppMessageRequest
{
    /// <summary>
    /// Gets the recipient's WhatsApp phone number in E.164 format (an optional leading '+'
    /// followed only by digits).
    /// </summary>
    public required string To { get; init; }

    /// <summary>
    /// Gets the message this request is replying to, if any. When set, the recipient's client
    /// renders this message as an inline reply/quote of the referenced message.
    /// </summary>
    public WhatsAppReplyContext? Context { get; init; }

    /// <summary>
    /// Gets the Graph API message type discriminator (e.g. <c>"text"</c>, <c>"template"</c>),
    /// which also doubles as the JSON property name under which <see cref="BuildTypePayload"/>
    /// is nested.
    /// </summary>
    internal abstract string Type { get; }

    /// <summary>
    /// Validates the type-specific fields of this request, throwing
    /// <see cref="Errors.WhatsAppValidationException"/> if invalid. Common fields (<see cref="To"/>)
    /// are validated separately by <see cref="ToJsonPayload"/>.
    /// </summary>
    internal abstract void Validate();

    /// <summary>
    /// Builds the type-specific JSON payload nested under the <see cref="Type"/> key, e.g. for a
    /// text message: <c>{ "body": "...", "preview_url": false }</c>.
    /// </summary>
    internal abstract JsonNode BuildTypePayload();

    /// <summary>
    /// Validates this request and builds the full JSON payload to send to the Graph API
    /// <c>/messages</c> endpoint.
    /// </summary>
    /// <returns>The complete request payload.</returns>
    /// <exception cref="Errors.WhatsAppValidationException">The request is invalid.</exception>
    internal JsonObject ToJsonPayload()
    {
        Validate();
        var normalizedTo = PhoneNumberValidator.NormalizeRecipient(To, nameof(To));

        var payload = new JsonObject
        {
            ["messaging_product"] = "whatsapp",
            ["recipient_type"] = "individual",
            ["to"] = normalizedTo,
            ["type"] = Type,
            [Type] = BuildTypePayload(),
        };

        if (Context is { MessageId: { Length: > 0 } messageId })
        {
            payload["context"] = new JsonObject { ["message_id"] = messageId };
        }

        return payload;
    }
}
