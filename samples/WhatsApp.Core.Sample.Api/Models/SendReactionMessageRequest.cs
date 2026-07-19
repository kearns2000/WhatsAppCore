namespace WhatsApp.Core.Sample.Api.Models;

/// <summary>
/// Request body for sending (or removing) an emoji reaction.
/// </summary>
public sealed class SendReactionMessageRequest
{
    /// <summary>Recipient phone number in E.164 format.</summary>
    public required string To { get; init; }

    /// <summary>The WhatsApp message id (<c>wamid...</c>) of the message being reacted to.</summary>
    public required string MessageId { get; init; }

    /// <summary>The emoji to react with, or empty/null to remove a previous reaction.</summary>
    public string? Emoji { get; init; }
}
