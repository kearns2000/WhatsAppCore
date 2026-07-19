using WhatsApp.Core.Messages;
using WhatsApp.Core.Responses;

namespace WhatsApp.Core.Client;

/// <summary>
/// Convenience extension methods for sending each supported message type without constructing
/// the corresponding <see cref="WhatsAppMessageRequest"/> subclass by hand.
/// </summary>
public static class WhatsAppClientExtensions
{
    /// <summary>
    /// Sends a free-form text message.
    /// </summary>
    /// <param name="client">The client to send from.</param>
    /// <param name="to">The recipient's phone number, in E.164 format.</param>
    /// <param name="body">The text content of the message.</param>
    /// <param name="previewUrl">Whether to render a preview for the first URL found in <paramref name="body"/>.</param>
    /// <param name="context">The message being replied to, if any.</param>
    /// <param name="stopToken">A token used to cancel the operation.</param>
    public static Task<SendMessageResponse> SendTextAsync(
        this IWhatsAppClient client,
        string to,
        string body,
        bool previewUrl = false,
        WhatsAppReplyContext? context = null,
        CancellationToken stopToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.SendMessageAsync(
            new TextMessageRequest { To = to, Body = body, PreviewUrl = previewUrl, Context = context },
            stopToken);
    }

    /// <summary>
    /// Sends a pre-approved template message.
    /// </summary>
    /// <param name="client">The client to send from.</param>
    /// <param name="to">The recipient's phone number, in E.164 format.</param>
    /// <param name="templateName">The name of the approved template.</param>
    /// <param name="languageCode">The template's language/locale code, e.g. <c>"en_US"</c>.</param>
    /// <param name="components">The components used to fill the template's placeholders, if any.</param>
    /// <param name="context">The message being replied to, if any.</param>
    /// <param name="stopToken">A token used to cancel the operation.</param>
    public static Task<SendMessageResponse> SendTemplateAsync(
        this IWhatsAppClient client,
        string to,
        string templateName,
        string languageCode,
        IReadOnlyList<WhatsAppTemplateComponent>? components = null,
        WhatsAppReplyContext? context = null,
        CancellationToken stopToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.SendMessageAsync(
            new TemplateMessageRequest { To = to, Name = templateName, LanguageCode = languageCode, Components = components, Context = context },
            stopToken);
    }

    /// <summary>
    /// Sends an image message, referencing media either by id or by link.
    /// </summary>
    /// <param name="client">The client to send from.</param>
    /// <param name="to">The recipient's phone number, in E.164 format.</param>
    /// <param name="mediaId">The previously uploaded media id. Exactly one of this or <paramref name="link"/> must be set.</param>
    /// <param name="link">The publicly reachable image URL. Exactly one of this or <paramref name="mediaId"/> must be set.</param>
    /// <param name="caption">The optional caption displayed alongside the image.</param>
    /// <param name="context">The message being replied to, if any.</param>
    /// <param name="stopToken">A token used to cancel the operation.</param>
    public static Task<SendMessageResponse> SendImageAsync(
        this IWhatsAppClient client,
        string to,
        string? mediaId = null,
        string? link = null,
        string? caption = null,
        WhatsAppReplyContext? context = null,
        CancellationToken stopToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.SendMessageAsync(
            new ImageMessageRequest { To = to, MediaId = mediaId, Link = link, Caption = caption, Context = context },
            stopToken);
    }

    /// <summary>
    /// Sends a document message, referencing media either by id or by link.
    /// </summary>
    /// <param name="client">The client to send from.</param>
    /// <param name="to">The recipient's phone number, in E.164 format.</param>
    /// <param name="mediaId">The previously uploaded media id. Exactly one of this or <paramref name="link"/> must be set.</param>
    /// <param name="link">The publicly reachable document URL. Exactly one of this or <paramref name="mediaId"/> must be set.</param>
    /// <param name="caption">The optional caption displayed alongside the document.</param>
    /// <param name="fileName">The optional file name suggested to the recipient.</param>
    /// <param name="context">The message being replied to, if any.</param>
    /// <param name="stopToken">A token used to cancel the operation.</param>
    public static Task<SendMessageResponse> SendDocumentAsync(
        this IWhatsAppClient client,
        string to,
        string? mediaId = null,
        string? link = null,
        string? caption = null,
        string? fileName = null,
        WhatsAppReplyContext? context = null,
        CancellationToken stopToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.SendMessageAsync(
            new DocumentMessageRequest { To = to, MediaId = mediaId, Link = link, Caption = caption, FileName = fileName, Context = context },
            stopToken);
    }

    /// <summary>
    /// Sends an audio message, referencing media either by id or by link.
    /// </summary>
    /// <param name="client">The client to send from.</param>
    /// <param name="to">The recipient's phone number, in E.164 format.</param>
    /// <param name="mediaId">The previously uploaded media id. Exactly one of this or <paramref name="link"/> must be set.</param>
    /// <param name="link">The publicly reachable audio URL. Exactly one of this or <paramref name="mediaId"/> must be set.</param>
    /// <param name="context">The message being replied to, if any.</param>
    /// <param name="stopToken">A token used to cancel the operation.</param>
    public static Task<SendMessageResponse> SendAudioAsync(
        this IWhatsAppClient client,
        string to,
        string? mediaId = null,
        string? link = null,
        WhatsAppReplyContext? context = null,
        CancellationToken stopToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.SendMessageAsync(
            new AudioMessageRequest { To = to, MediaId = mediaId, Link = link, Context = context },
            stopToken);
    }

    /// <summary>
    /// Sends a video message, referencing media either by id or by link.
    /// </summary>
    /// <param name="client">The client to send from.</param>
    /// <param name="to">The recipient's phone number, in E.164 format.</param>
    /// <param name="mediaId">The previously uploaded media id. Exactly one of this or <paramref name="link"/> must be set.</param>
    /// <param name="link">The publicly reachable video URL. Exactly one of this or <paramref name="mediaId"/> must be set.</param>
    /// <param name="caption">The optional caption displayed alongside the video.</param>
    /// <param name="context">The message being replied to, if any.</param>
    /// <param name="stopToken">A token used to cancel the operation.</param>
    public static Task<SendMessageResponse> SendVideoAsync(
        this IWhatsAppClient client,
        string to,
        string? mediaId = null,
        string? link = null,
        string? caption = null,
        WhatsAppReplyContext? context = null,
        CancellationToken stopToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.SendMessageAsync(
            new VideoMessageRequest { To = to, MediaId = mediaId, Link = link, Caption = caption, Context = context },
            stopToken);
    }

    /// <summary>
    /// Sends a sticker message, referencing media either by id or by link.
    /// </summary>
    /// <param name="client">The client to send from.</param>
    /// <param name="to">The recipient's phone number, in E.164 format.</param>
    /// <param name="mediaId">The previously uploaded media id. Exactly one of this or <paramref name="link"/> must be set.</param>
    /// <param name="link">The publicly reachable sticker (WebP) URL. Exactly one of this or <paramref name="mediaId"/> must be set.</param>
    /// <param name="context">The message being replied to, if any.</param>
    /// <param name="stopToken">A token used to cancel the operation.</param>
    public static Task<SendMessageResponse> SendStickerAsync(
        this IWhatsAppClient client,
        string to,
        string? mediaId = null,
        string? link = null,
        WhatsAppReplyContext? context = null,
        CancellationToken stopToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.SendMessageAsync(
            new StickerMessageRequest { To = to, MediaId = mediaId, Link = link, Context = context },
            stopToken);
    }

    /// <summary>
    /// Sends a pin-drop location message.
    /// </summary>
    /// <param name="client">The client to send from.</param>
    /// <param name="to">The recipient's phone number, in E.164 format.</param>
    /// <param name="latitude">The latitude of the location, in degrees.</param>
    /// <param name="longitude">The longitude of the location, in degrees.</param>
    /// <param name="name">The optional display name of the location.</param>
    /// <param name="address">The optional display address of the location.</param>
    /// <param name="context">The message being replied to, if any.</param>
    /// <param name="stopToken">A token used to cancel the operation.</param>
    public static Task<SendMessageResponse> SendLocationAsync(
        this IWhatsAppClient client,
        string to,
        double latitude,
        double longitude,
        string? name = null,
        string? address = null,
        WhatsAppReplyContext? context = null,
        CancellationToken stopToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.SendMessageAsync(
            new LocationMessageRequest { To = to, Latitude = latitude, Longitude = longitude, Name = name, Address = address, Context = context },
            stopToken);
    }

    /// <summary>
    /// Sends one or more contact cards.
    /// </summary>
    /// <param name="client">The client to send from.</param>
    /// <param name="to">The recipient's phone number, in E.164 format.</param>
    /// <param name="contacts">The contact cards to share.</param>
    /// <param name="context">The message being replied to, if any.</param>
    /// <param name="stopToken">A token used to cancel the operation.</param>
    public static Task<SendMessageResponse> SendContactsAsync(
        this IWhatsAppClient client,
        string to,
        IReadOnlyList<WhatsAppContact> contacts,
        WhatsAppReplyContext? context = null,
        CancellationToken stopToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.SendMessageAsync(
            new ContactsMessageRequest { To = to, Contacts = contacts, Context = context },
            stopToken);
    }

    /// <summary>
    /// Sends an interactive message (reply buttons, a list, or a call-to-action URL button).
    /// </summary>
    /// <param name="client">The client to send from.</param>
    /// <param name="to">The recipient's phone number, in E.164 format.</param>
    /// <param name="action">The interactive action to present.</param>
    /// <param name="bodyText">The body text of the message.</param>
    /// <param name="header">The optional header.</param>
    /// <param name="footer">The optional footer.</param>
    /// <param name="context">The message being replied to, if any.</param>
    /// <param name="stopToken">A token used to cancel the operation.</param>
    public static Task<SendMessageResponse> SendInteractiveAsync(
        this IWhatsAppClient client,
        string to,
        WhatsAppInteractiveAction action,
        string bodyText,
        WhatsAppInteractiveHeader? header = null,
        WhatsAppInteractiveFooter? footer = null,
        WhatsAppReplyContext? context = null,
        CancellationToken stopToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.SendMessageAsync(
            new InteractiveMessageRequest
            {
                To = to,
                Action = action,
                Body = new WhatsAppInteractiveBody { Text = bodyText },
                Header = header,
                Footer = footer,
                Context = context,
            },
            stopToken);
    }

    /// <summary>
    /// Sends (or removes, when <paramref name="emoji"/> is empty) an emoji reaction to a
    /// previously received message.
    /// </summary>
    /// <param name="client">The client to send from.</param>
    /// <param name="to">The recipient's phone number, in E.164 format.</param>
    /// <param name="messageId">The WhatsApp message id (<c>wamid...</c>) of the message being reacted to.</param>
    /// <param name="emoji">The emoji to react with, or <see langword="null"/>/empty to remove a previous reaction.</param>
    /// <param name="stopToken">A token used to cancel the operation.</param>
    public static Task<SendMessageResponse> SendReactionAsync(
        this IWhatsAppClient client,
        string to,
        string messageId,
        string? emoji,
        CancellationToken stopToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.SendMessageAsync(
            new ReactionMessageRequest { To = to, MessageId = messageId, Emoji = emoji },
            stopToken);
    }
}
