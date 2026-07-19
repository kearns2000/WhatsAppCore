using System.Text.Json.Nodes;
using WhatsApp.Core.Errors;

namespace WhatsApp.Core.Messages;

/// <summary>
/// A single component of a template message (a header, body, or button) carrying the
/// parameters to substitute into that component's placeholders.
/// </summary>
public sealed record WhatsAppTemplateComponent
{
    /// <summary>
    /// Gets the component type: <c>"header"</c>, <c>"body"</c>, or <c>"button"</c>.
    /// </summary>
    public required string ComponentType { get; init; }

    /// <summary>
    /// Gets the button subtype (<c>"quick_reply"</c> or <c>"url"</c>), required only when
    /// <see cref="ComponentType"/> is <c>"button"</c>.
    /// </summary>
    public string? SubType { get; init; }

    /// <summary>
    /// Gets the zero-based button index, required only when <see cref="ComponentType"/> is
    /// <c>"button"</c>.
    /// </summary>
    public int? Index { get; init; }

    /// <summary>
    /// Gets the parameters to substitute into this component's placeholders.
    /// </summary>
    public IReadOnlyList<WhatsAppTemplateParameter>? Parameters { get; init; }

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(ComponentType))
        {
            throw new WhatsAppValidationException("A template component must specify a component type.");
        }

        if (string.Equals(ComponentType, "button", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(SubType))
            {
                throw new WhatsAppValidationException("A button template component must specify a sub-type.");
            }

            if (Index is null or < 0)
            {
                throw new WhatsAppValidationException("A button template component must specify a non-negative index.");
            }
        }
    }

    internal JsonObject ToJson()
    {
        var obj = new JsonObject { ["type"] = ComponentType };

        if (SubType is not null)
        {
            obj["sub_type"] = SubType;
        }

        if (Index is not null)
        {
            obj["index"] = Index.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (Parameters is { Count: > 0 })
        {
            var array = new JsonArray();
            foreach (var parameter in Parameters)
            {
                array.Add(parameter.ToJson());
            }

            obj["parameters"] = array;
        }

        return obj;
    }
}
