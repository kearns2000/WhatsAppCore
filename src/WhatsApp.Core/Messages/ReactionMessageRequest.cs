using System.Text.Json.Nodes;
using WhatsApp.Core.Errors;

namespace WhatsApp.Core.Messages;

/// <summary>
/// A reaction (emoji) applied to a previously received message.
/// </summary>
public sealed record ReactionMessageRequest : WhatsAppMessageRequest
{
    /// <summary>
    /// Gets the WhatsApp message id (<c>wamid...</c>) of the message being reacted to.
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    /// Gets the emoji to react with. Set to <see langword="null"/> or an empty string to remove
    /// a previously sent reaction.
    /// </summary>
    public string? Emoji { get; init; }

    internal override string Type => "reaction";

    internal override void Validate()
    {
        if (string.IsNullOrWhiteSpace(MessageId))
        {
            throw new WhatsAppValidationException("A reaction message must specify the id of the message being reacted to.");
        }
    }

    internal override JsonNode BuildTypePayload() => new JsonObject
    {
        ["message_id"] = MessageId,
        ["emoji"] = Emoji ?? string.Empty,
    };
}
