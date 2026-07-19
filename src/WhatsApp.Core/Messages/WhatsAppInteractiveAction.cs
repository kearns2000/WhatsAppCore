using System.Text.Json.Nodes;
using WhatsApp.Core.Errors;
using WhatsApp.Core.Internal;

namespace WhatsApp.Core.Messages;

/// <summary>
/// Base type for the action of an <see cref="InteractiveMessageRequest"/>: reply buttons, a
/// list, or a call-to-action URL button.
/// </summary>
public abstract record WhatsAppInteractiveAction
{
    /// <summary>
    /// Gets the Graph API interactive type discriminator, e.g. <c>"button"</c>, <c>"list"</c>,
    /// or <c>"cta_url"</c>.
    /// </summary>
    internal abstract string Kind { get; }

    /// <summary>
    /// Validates this action's content, throwing <see cref="WhatsAppValidationException"/> if invalid.
    /// </summary>
    internal abstract void Validate();

    /// <summary>
    /// Builds the JSON payload for the <c>action</c> field.
    /// </summary>
    internal abstract JsonObject ToJson();
}

/// <summary>
/// An interactive action presenting up to three quick-reply buttons.
/// </summary>
public sealed record WhatsAppInteractiveButtonsAction : WhatsAppInteractiveAction
{
    /// <summary>
    /// Gets the reply buttons to present. Must contain between one and
    /// <see cref="WhatsAppLimits.MaxInteractiveButtons"/> entries.
    /// </summary>
    public required IReadOnlyList<WhatsAppInteractiveButton> Buttons { get; init; }

    internal override string Kind => "button";

    internal override void Validate()
    {
        if (Buttons is null or { Count: 0 })
        {
            throw new WhatsAppValidationException("An interactive buttons action must include at least one button.");
        }

        if (Buttons.Count > WhatsAppLimits.MaxInteractiveButtons)
        {
            throw new WhatsAppValidationException(
                $"An interactive buttons action must not include more than {WhatsAppLimits.MaxInteractiveButtons} buttons.");
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var button in Buttons)
        {
            if (string.IsNullOrWhiteSpace(button.Id))
            {
                throw new WhatsAppValidationException("Each interactive button must have a non-empty id.");
            }

            if (string.IsNullOrWhiteSpace(button.Title))
            {
                throw new WhatsAppValidationException("Each interactive button must have a non-empty title.");
            }

            if (!ids.Add(button.Id))
            {
                throw new WhatsAppValidationException($"Duplicate interactive button id '{button.Id}'.");
            }
        }
    }

    internal override JsonObject ToJson()
    {
        var array = new JsonArray();
        foreach (var button in Buttons)
        {
            array.Add(button.ToJson());
        }

        return new JsonObject { ["buttons"] = array };
    }
}

/// <summary>
/// A single quick-reply button.
/// </summary>
public sealed record WhatsAppInteractiveButton
{
    /// <summary>Gets the developer-defined id returned when the button is tapped.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the button's display text.</summary>
    public required string Title { get; init; }

    internal JsonObject ToJson() => new()
    {
        ["type"] = "reply",
        ["reply"] = new JsonObject { ["id"] = Id, ["title"] = Title },
    };
}

/// <summary>
/// An interactive action presenting a single-select list, grouped into sections.
/// </summary>
public sealed record WhatsAppInteractiveListAction : WhatsAppInteractiveAction
{
    /// <summary>
    /// Gets the text displayed on the button that opens the list.
    /// </summary>
    public required string ButtonText { get; init; }

    /// <summary>
    /// Gets the sections making up the list. Must contain at least one section, and no more
    /// than <see cref="WhatsAppLimits.MaxInteractiveListSections"/> rows across all sections.
    /// </summary>
    public required IReadOnlyList<WhatsAppInteractiveSection> Sections { get; init; }

    internal override string Kind => "list";

    internal override void Validate()
    {
        if (string.IsNullOrWhiteSpace(ButtonText))
        {
            throw new WhatsAppValidationException("An interactive list action must specify button text.");
        }

        if (Sections is null or { Count: 0 })
        {
            throw new WhatsAppValidationException("An interactive list action must include at least one section.");
        }

        if (Sections.Count > WhatsAppLimits.MaxInteractiveListSections)
        {
            throw new WhatsAppValidationException(
                $"An interactive list action must not include more than {WhatsAppLimits.MaxInteractiveListSections} sections.");
        }

        var totalRows = 0;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var section in Sections)
        {
            if (section.Rows is null or { Count: 0 })
            {
                throw new WhatsAppValidationException("Each interactive list section must include at least one row.");
            }

            totalRows += section.Rows.Count;
            foreach (var row in section.Rows)
            {
                if (string.IsNullOrWhiteSpace(row.Id))
                {
                    throw new WhatsAppValidationException("Each interactive list row must have a non-empty id.");
                }

                if (string.IsNullOrWhiteSpace(row.Title))
                {
                    throw new WhatsAppValidationException("Each interactive list row must have a non-empty title.");
                }

                if (!ids.Add(row.Id))
                {
                    throw new WhatsAppValidationException($"Duplicate interactive list row id '{row.Id}'.");
                }
            }
        }

        if (totalRows > WhatsAppLimits.MaxInteractiveListRows)
        {
            throw new WhatsAppValidationException(
                $"An interactive list action must not include more than {WhatsAppLimits.MaxInteractiveListRows} rows in total.");
        }
    }

    internal override JsonObject ToJson()
    {
        var sections = new JsonArray();
        foreach (var section in Sections)
        {
            sections.Add(section.ToJson());
        }

        return new JsonObject
        {
            ["button"] = ButtonText,
            ["sections"] = sections,
        };
    }
}

/// <summary>
/// A named group of selectable rows within an interactive list.
/// </summary>
public sealed record WhatsAppInteractiveSection
{
    /// <summary>Gets the section's title, shown above its rows.</summary>
    public string? Title { get; init; }

    /// <summary>Gets the selectable rows within this section.</summary>
    public required IReadOnlyList<WhatsAppInteractiveRow> Rows { get; init; }

    internal JsonObject ToJson()
    {
        var rows = new JsonArray();
        foreach (var row in Rows)
        {
            rows.Add(row.ToJson());
        }

        var obj = new JsonObject();
        if (Title is not null)
        {
            obj["title"] = Title;
        }

        obj["rows"] = rows;
        return obj;
    }
}

/// <summary>
/// A single selectable row within an interactive list section.
/// </summary>
public sealed record WhatsAppInteractiveRow
{
    /// <summary>Gets the developer-defined id returned when the row is selected.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the row's display title.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the row's optional secondary description text.</summary>
    public string? Description { get; init; }

    internal JsonObject ToJson()
    {
        var obj = new JsonObject { ["id"] = Id, ["title"] = Title };
        if (Description is not null)
        {
            obj["description"] = Description;
        }

        return obj;
    }
}

/// <summary>
/// An interactive action presenting a single call-to-action button that opens a URL.
/// </summary>
public sealed record WhatsAppInteractiveCtaUrlAction : WhatsAppInteractiveAction
{
    /// <summary>Gets the button's display text.</summary>
    public required string DisplayText { get; init; }

    /// <summary>Gets the URL opened when the button is tapped.</summary>
    public required string Url { get; init; }

    internal override string Kind => "cta_url";

    internal override void Validate()
    {
        if (string.IsNullOrWhiteSpace(DisplayText))
        {
            throw new WhatsAppValidationException("An interactive call-to-action button must specify display text.");
        }

        if (string.IsNullOrWhiteSpace(Url) || !Uri.TryCreate(Url, UriKind.Absolute, out _))
        {
            throw new WhatsAppValidationException("An interactive call-to-action button must specify a valid absolute URL.");
        }
    }

    internal override JsonObject ToJson() => new()
    {
        ["name"] = "cta_url",
        ["parameters"] = new JsonObject { ["display_text"] = DisplayText, ["url"] = Url },
    };
}
