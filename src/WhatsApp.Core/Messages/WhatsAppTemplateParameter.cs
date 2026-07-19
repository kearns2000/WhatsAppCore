using System.Text.Json.Nodes;
using WhatsApp.Core.Errors;

namespace WhatsApp.Core.Messages;

/// <summary>
/// A single parameter substituted into a template component (e.g. one <c>{{1}}</c> placeholder).
/// </summary>
public sealed record WhatsAppTemplateParameter
{
    private WhatsAppTemplateParameter(string type)
    {
        Type = type;
    }

    /// <summary>
    /// Gets the Graph API parameter type discriminator, e.g. <c>"text"</c> or <c>"currency"</c>.
    /// </summary>
    public string Type { get; }

    /// <summary>Gets the plain text value, when <see cref="Type"/> is <c>"text"</c>.</summary>
    public string? Text { get; private init; }

    /// <summary>Gets the button payload value, when <see cref="Type"/> is <c>"payload"</c>.</summary>
    public string? Payload { get; private init; }

    /// <summary>Gets the currency value, when <see cref="Type"/> is <c>"currency"</c>.</summary>
    public WhatsAppTemplateCurrency? Currency { get; private init; }

    /// <summary>Gets the date/time value, when <see cref="Type"/> is <c>"date_time"</c>.</summary>
    public string? DateTimeText { get; private init; }

    /// <summary>Gets the media reference, when <see cref="Type"/> is <c>"image"</c>, <c>"video"</c>, or <c>"document"</c>.</summary>
    public WhatsAppTemplateMediaReference? Media { get; private init; }

    /// <summary>
    /// Creates a text substitution parameter.
    /// </summary>
    /// <param name="text">The text to substitute.</param>
    public static WhatsAppTemplateParameter ForText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            throw new WhatsAppValidationException("A template text parameter must not be empty.");
        }

        return new WhatsAppTemplateParameter("text") { Text = text };
    }

    /// <summary>
    /// Creates a quick-reply or URL button payload parameter.
    /// </summary>
    /// <param name="payload">The button payload.</param>
    public static WhatsAppTemplateParameter ForPayload(string payload)
    {
        if (string.IsNullOrEmpty(payload))
        {
            throw new WhatsAppValidationException("A template button payload parameter must not be empty.");
        }

        return new WhatsAppTemplateParameter("payload") { Payload = payload };
    }

    /// <summary>
    /// Creates a currency substitution parameter.
    /// </summary>
    /// <param name="currency">The currency value.</param>
    public static WhatsAppTemplateParameter ForCurrency(WhatsAppTemplateCurrency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        return new WhatsAppTemplateParameter("currency") { Currency = currency };
    }

    /// <summary>
    /// Creates a date/time substitution parameter.
    /// </summary>
    /// <param name="fallbackValue">The fallback, human-readable date/time text.</param>
    public static WhatsAppTemplateParameter ForDateTime(string fallbackValue)
    {
        if (string.IsNullOrEmpty(fallbackValue))
        {
            throw new WhatsAppValidationException("A template date/time parameter must not be empty.");
        }

        return new WhatsAppTemplateParameter("date_time") { DateTimeText = fallbackValue };
    }

    /// <summary>
    /// Creates an image media parameter.
    /// </summary>
    /// <param name="media">The image media reference.</param>
    public static WhatsAppTemplateParameter ForImage(WhatsAppTemplateMediaReference media) =>
        new("image") { Media = media ?? throw new ArgumentNullException(nameof(media)) };

    /// <summary>
    /// Creates a video media parameter.
    /// </summary>
    /// <param name="media">The video media reference.</param>
    public static WhatsAppTemplateParameter ForVideo(WhatsAppTemplateMediaReference media) =>
        new("video") { Media = media ?? throw new ArgumentNullException(nameof(media)) };

    /// <summary>
    /// Creates a document media parameter.
    /// </summary>
    /// <param name="media">The document media reference.</param>
    public static WhatsAppTemplateParameter ForDocument(WhatsAppTemplateMediaReference media) =>
        new("document") { Media = media ?? throw new ArgumentNullException(nameof(media)) };

    internal JsonObject ToJson()
    {
        var obj = new JsonObject { ["type"] = Type };
        switch (Type)
        {
            case "text":
                obj["text"] = Text;
                break;
            case "payload":
                obj["payload"] = Payload;
                break;
            case "currency":
                obj["currency"] = Currency!.ToJson();
                break;
            case "date_time":
                obj["date_time"] = new JsonObject { ["fallback_value"] = DateTimeText };
                break;
            case "image":
                obj["image"] = Media!.ToJson();
                break;
            case "video":
                obj["video"] = Media!.ToJson();
                break;
            case "document":
                obj["document"] = Media!.ToJson();
                break;
        }

        return obj;
    }
}

/// <summary>
/// A currency value substituted into a template, e.g. <c>$100.00</c>.
/// </summary>
public sealed record WhatsAppTemplateCurrency
{
    /// <summary>
    /// Gets the fallback, human-readable value shown if the recipient's client cannot localize
    /// the currency (e.g. <c>"$100.00"</c>).
    /// </summary>
    public required string FallbackValue { get; init; }

    /// <summary>
    /// Gets the ISO 4217 currency code, e.g. <c>"USD"</c>.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the amount, multiplied by 1000 (e.g. $100.00 is represented as 100000).
    /// </summary>
    public required long Amount1000 { get; init; }

    internal JsonObject ToJson() => new()
    {
        ["fallback_value"] = FallbackValue,
        ["code"] = Code,
        ["amount_1000"] = Amount1000,
    };
}

/// <summary>
/// A reference to media (by id or link) substituted into a template header parameter.
/// </summary>
public sealed record WhatsAppTemplateMediaReference
{
    /// <summary>Gets the previously uploaded media id, if referencing media by id.</summary>
    public string? Id { get; init; }

    /// <summary>Gets the publicly reachable media link, if referencing media by URL.</summary>
    public string? Link { get; init; }

    internal JsonObject ToJson()
    {
        Internal.MediaReferenceValidator.Validate(Id, Link, "template media parameter");
        var obj = new JsonObject();
        if (Id is not null)
        {
            obj["id"] = Id;
        }
        else
        {
            obj["link"] = Link;
        }

        return obj;
    }
}
