using System.Text.Json.Nodes;
using WhatsApp.Core.Errors;
using WhatsApp.Core.Internal;

namespace WhatsApp.Core.Messages;

/// <summary>
/// An interactive message: reply buttons, a selectable list, or a call-to-action URL button,
/// optionally with a header, body, and footer.
/// </summary>
public sealed record InteractiveMessageRequest : WhatsAppMessageRequest
{
    /// <summary>
    /// Gets the interactive action: <see cref="WhatsAppInteractiveButtonsAction"/>,
    /// <see cref="WhatsAppInteractiveListAction"/>, or <see cref="WhatsAppInteractiveCtaUrlAction"/>.
    /// </summary>
    public required WhatsAppInteractiveAction Action { get; init; }

    /// <summary>
    /// Gets the optional header.
    /// </summary>
    public WhatsAppInteractiveHeader? Header { get; init; }

    /// <summary>
    /// Gets the body text. Required by the Graph API for all interactive message types.
    /// </summary>
    public required WhatsAppInteractiveBody Body { get; init; }

    /// <summary>
    /// Gets the optional footer.
    /// </summary>
    public WhatsAppInteractiveFooter? Footer { get; init; }

    internal override string Type => "interactive";

    internal override void Validate()
    {
        if (string.IsNullOrEmpty(Body.Text))
        {
            throw new WhatsAppValidationException("An interactive message must include non-empty body text.");
        }

        if (Body.Text.Length > WhatsAppLimits.MaxInteractiveTextLength)
        {
            throw new WhatsAppValidationException(
                $"An interactive message body must not exceed {WhatsAppLimits.MaxInteractiveTextLength} characters.");
        }

        Header?.Validate();

        if (Footer is { Text: { Length: 0 } })
        {
            throw new WhatsAppValidationException("An interactive message footer, when present, must not be empty.");
        }

        Action.Validate();
    }

    internal override JsonNode BuildTypePayload()
    {
        var obj = new JsonObject
        {
            ["type"] = Action.Kind,
            ["body"] = new JsonObject { ["text"] = Body.Text },
        };

        if (Header is not null)
        {
            obj["header"] = Header.ToJson();
        }

        if (Footer is not null)
        {
            obj["footer"] = new JsonObject { ["text"] = Footer.Text };
        }

        obj["action"] = Action.ToJson();
        return obj;
    }
}
