using System.Text.Json.Nodes;
using WhatsApp.Core.Errors;

namespace WhatsApp.Core.Messages;

/// <summary>
/// A pre-approved message template, required for sending business-initiated conversations
/// outside the 24-hour customer service window.
/// </summary>
public sealed record TemplateMessageRequest : WhatsAppMessageRequest
{
    /// <summary>
    /// Gets the name of the approved template, as configured in WhatsApp Manager.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the template's language/locale code, e.g. <c>"en_US"</c>.
    /// </summary>
    public required string LanguageCode { get; init; }

    /// <summary>
    /// Gets the components (header/body/button parameters) used to fill the template's
    /// placeholders. May be omitted for templates with no placeholders.
    /// </summary>
    public IReadOnlyList<WhatsAppTemplateComponent>? Components { get; init; }

    internal override string Type => "template";

    internal override void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new WhatsAppValidationException("A template message must specify a template name.");
        }

        if (string.IsNullOrWhiteSpace(LanguageCode))
        {
            throw new WhatsAppValidationException("A template message must specify a language code.");
        }

        if (Components is not null)
        {
            foreach (var component in Components)
            {
                component.Validate();
            }
        }
    }

    internal override JsonNode BuildTypePayload()
    {
        var payload = new JsonObject
        {
            ["name"] = Name,
            ["language"] = new JsonObject { ["code"] = LanguageCode },
        };

        if (Components is { Count: > 0 })
        {
            var array = new JsonArray();
            foreach (var component in Components)
            {
                array.Add(component.ToJson());
            }

            payload["components"] = array;
        }

        return payload;
    }
}
