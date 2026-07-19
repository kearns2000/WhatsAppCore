using System.Text.Json.Nodes;
using WhatsApp.Core.Errors;
using WhatsApp.Core.Internal;

namespace WhatsApp.Core.Messages;

/// <summary>
/// A free-form text message.
/// </summary>
public sealed record TextMessageRequest : WhatsAppMessageRequest
{
    /// <summary>
    /// Gets the text content of the message. Must be non-empty and at most
    /// <see cref="WhatsAppLimits.MaxTextBodyLength"/> characters.
    /// </summary>
    public required string Body { get; init; }

    /// <summary>
    /// Gets a value indicating whether the recipient's client should render a preview for the
    /// first URL found in <see cref="Body"/>. Defaults to <see langword="false"/>.
    /// </summary>
    public bool PreviewUrl { get; init; }

    internal override string Type => "text";

    internal override void Validate()
    {
        if (string.IsNullOrEmpty(Body))
        {
            throw new WhatsAppValidationException("A text message body must not be empty.");
        }

        if (Body.Length > WhatsAppLimits.MaxTextBodyLength)
        {
            throw new WhatsAppValidationException(
                $"A text message body must not exceed {WhatsAppLimits.MaxTextBodyLength} characters.");
        }
    }

    internal override JsonNode BuildTypePayload() => new JsonObject
    {
        ["body"] = Body,
        ["preview_url"] = PreviewUrl,
    };
}
