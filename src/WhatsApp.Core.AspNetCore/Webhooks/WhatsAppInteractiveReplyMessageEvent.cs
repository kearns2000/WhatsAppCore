namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// An inbound reply to an interactive message (a button reply or a list reply) previously sent
/// by this application.
/// </summary>
public sealed record WhatsAppInteractiveReplyMessageEvent : WhatsAppMessageEvent
{
    /// <summary>
    /// Gets the interactive reply subtype reported by Meta, e.g. <c>"button_reply"</c> or
    /// <c>"list_reply"</c>.
    /// </summary>
    public required string InteractiveType { get; init; }

    /// <summary>
    /// Gets the id of the selected button or list row, as originally defined when the
    /// interactive message was sent.
    /// </summary>
    public required string ReplyId { get; init; }

    /// <summary>
    /// Gets the display title of the selected button or list row.
    /// </summary>
    public required string ReplyTitle { get; init; }

    /// <summary>
    /// Gets the description of the selected list row, if <see cref="InteractiveType"/> is
    /// <c>"list_reply"</c> and a description was configured.
    /// </summary>
    public string? ReplyDescription { get; init; }
}
