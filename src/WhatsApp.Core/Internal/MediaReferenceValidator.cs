using WhatsApp.Core.Errors;

namespace WhatsApp.Core.Internal;

/// <summary>
/// Validates that a media-bearing message specifies exactly one of an uploaded media id or a
/// publicly reachable link, since the Graph API rejects payloads that specify both or neither.
/// </summary>
internal static class MediaReferenceValidator
{
    /// <summary>
    /// Ensures that exactly one of <paramref name="mediaId"/> or <paramref name="link"/> is set.
    /// </summary>
    /// <param name="mediaId">The previously uploaded media id, if any.</param>
    /// <param name="link">The publicly reachable media URL, if any.</param>
    /// <param name="messageType">The message type being validated, used in error messages.</param>
    /// <exception cref="WhatsAppValidationException">Both or neither of the values were set.</exception>
    public static void Validate(string? mediaId, string? link, string messageType)
    {
        var hasId = !string.IsNullOrWhiteSpace(mediaId);
        var hasLink = !string.IsNullOrWhiteSpace(link);

        if (hasId && hasLink)
        {
            throw new WhatsAppValidationException(
                $"A '{messageType}' message must not specify both a media id and a link. Provide exactly one.");
        }

        if (!hasId && !hasLink)
        {
            throw new WhatsAppValidationException(
                $"A '{messageType}' message must specify either a media id or a link.");
        }
    }
}
