namespace WhatsApp.Core.Sample.Api.Models;

/// <summary>
/// Request body for sending a pre-approved template message.
/// </summary>
public sealed class SendTemplateMessageRequest
{
    /// <summary>Recipient phone number in E.164 format.</summary>
    public required string To { get; init; }

    /// <summary>The approved template name.</summary>
    public required string TemplateName { get; init; }

    /// <summary>The template language/locale code, e.g. <c>en_US</c>.</summary>
    public required string LanguageCode { get; init; }

    /// <summary>Optional body placeholder values, substituted in order into the template body.</summary>
    public IReadOnlyList<string>? BodyParameters { get; init; }

    /// <summary>Optional WhatsApp message id of the message being replied to.</summary>
    public string? ReplyToMessageId { get; init; }
}
