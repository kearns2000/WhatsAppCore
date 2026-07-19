using System.Globalization;
using System.Text.Json;
using WhatsApp.Core.AspNetCore.Serialization;
using WhatsApp.Core.AspNetCore.Serialization.Wire;
using WhatsApp.Core.AspNetCore.Webhooks;

namespace WhatsApp.Core.AspNetCore.Internal;

/// <summary>
/// Parses the raw JSON body of an inbound WhatsApp webhook delivery into a flat list of
/// strongly-typed <see cref="WhatsAppWebhookEvent"/> instances, one per message or status
/// update the delivery carries. Never throws for unrecognized message/status types or
/// unrecognized fields; both degrade to an explicit "unknown" event or are silently ignored,
/// respectively.
/// </summary>
internal static class WhatsAppWebhookEnvelopeParser
{
    /// <summary>
    /// Parses <paramref name="body"/> into zero or more webhook events.
    /// </summary>
    /// <param name="body">The exact raw JSON request body.</param>
    /// <param name="receivedAt">The instant the delivery was received, used both as the fallback timestamp and as <see cref="WhatsAppWebhookEvent.ReceivedAt"/>.</param>
    /// <returns>The parsed events, in the order they appeared in the payload.</returns>
    /// <exception cref="JsonException">The payload is not well-formed JSON, or does not match the expected top-level webhook envelope shape.</exception>
    public static IReadOnlyList<WhatsAppWebhookEvent> Parse(ReadOnlySpan<byte> body, DateTimeOffset receivedAt)
    {
        var envelope = JsonSerializer.Deserialize<WebhookEnvelopeWire>(body, WhatsAppWebhookJsonSerializerOptions.Default);
        if (envelope?.Entry is not { Count: > 0 })
        {
            return [];
        }

        List<WhatsAppWebhookEvent>? events = null;

        foreach (var entry in envelope.Entry)
        {
            if (string.IsNullOrEmpty(entry.Id) || entry.Changes is not { Count: > 0 })
            {
                continue;
            }

            foreach (var change in entry.Changes)
            {
                var value = change.Value;
                if (value is null)
                {
                    continue;
                }

                var phoneNumberId = value.Metadata?.PhoneNumberId;
                var displayPhoneNumber = value.Metadata?.DisplayPhoneNumber;

                if (value.Messages is { Count: > 0 } messages)
                {
                    foreach (var rawMessage in messages)
                    {
                        var messageEvent = ParseMessage(rawMessage, entry.Id, phoneNumberId, displayPhoneNumber, receivedAt);
                        if (messageEvent is not null)
                        {
                            (events ??= []).Add(messageEvent);
                        }
                    }
                }

                if (value.Statuses is { Count: > 0 } statuses)
                {
                    foreach (var rawStatus in statuses)
                    {
                        var statusEvent = ParseStatus(rawStatus, entry.Id, phoneNumberId, displayPhoneNumber, receivedAt);
                        if (statusEvent is not null)
                        {
                            (events ??= []).Add(statusEvent);
                        }
                    }
                }
            }
        }

        return events ?? (IReadOnlyList<WhatsAppWebhookEvent>)[];
    }

    private static WhatsAppMessageEvent? ParseMessage(
        JsonElement raw, string wabaId, string? phoneNumberId, string? displayPhoneNumber, DateTimeOffset receivedAt)
    {
        var wire = TryDeserialize<WebhookMessageWire>(raw);
        if (wire is null || string.IsNullOrEmpty(wire.Id) || string.IsNullOrEmpty(wire.From))
        {
            return null;
        }

        var timestamp = ParseUnixTimestamp(wire.Timestamp, receivedAt);
        var context = wire.Context is { Id: not null } wireContext
            ? new WhatsAppInboundReplyContext { MessageId = wireContext.Id, From = wireContext.From }
            : null;

        var type = wire.Type ?? string.Empty;

        if (type == "text" && wire.Text is { Body: not null } text)
        {
            return new WhatsAppTextMessageEvent
            {
                WhatsAppBusinessAccountId = wabaId,
                PhoneNumberId = phoneNumberId,
                DisplayPhoneNumber = displayPhoneNumber,
                ReceivedAt = receivedAt,
                ExtensionData = raw,
                MessageId = wire.Id,
                From = wire.From,
                Timestamp = timestamp,
                Context = context,
                Body = text.Body,
            };
        }

        if (type == "image" && wire.Image is { Id: not null } image)
        {
            return new WhatsAppImageMessageEvent
            {
                WhatsAppBusinessAccountId = wabaId,
                PhoneNumberId = phoneNumberId,
                DisplayPhoneNumber = displayPhoneNumber,
                ReceivedAt = receivedAt,
                ExtensionData = raw,
                MessageId = wire.Id,
                From = wire.From,
                Timestamp = timestamp,
                Context = context,
                Image = new WhatsAppInboundMedia
                {
                    MediaId = image.Id,
                    MimeType = image.MimeType,
                    Sha256Hash = image.Sha256,
                    Caption = image.Caption,
                },
            };
        }

        if (type == "document" && wire.Document is { Id: not null } document)
        {
            return new WhatsAppDocumentMessageEvent
            {
                WhatsAppBusinessAccountId = wabaId,
                PhoneNumberId = phoneNumberId,
                DisplayPhoneNumber = displayPhoneNumber,
                ReceivedAt = receivedAt,
                ExtensionData = raw,
                MessageId = wire.Id,
                From = wire.From,
                Timestamp = timestamp,
                Context = context,
                Document = new WhatsAppInboundMedia
                {
                    MediaId = document.Id,
                    MimeType = document.MimeType,
                    Sha256Hash = document.Sha256,
                    Caption = document.Caption,
                    FileName = document.Filename,
                },
            };
        }

        if (type == "audio" && wire.Audio is { Id: not null } audio)
        {
            return new WhatsAppAudioMessageEvent
            {
                WhatsAppBusinessAccountId = wabaId,
                PhoneNumberId = phoneNumberId,
                DisplayPhoneNumber = displayPhoneNumber,
                ReceivedAt = receivedAt,
                ExtensionData = raw,
                MessageId = wire.Id,
                From = wire.From,
                Timestamp = timestamp,
                Context = context,
                Audio = new WhatsAppInboundMedia
                {
                    MediaId = audio.Id,
                    MimeType = audio.MimeType,
                    Sha256Hash = audio.Sha256,
                },
                IsVoiceNote = audio.Voice ?? false,
            };
        }

        if (type == "video" && wire.Video is { Id: not null } video)
        {
            return new WhatsAppVideoMessageEvent
            {
                WhatsAppBusinessAccountId = wabaId,
                PhoneNumberId = phoneNumberId,
                DisplayPhoneNumber = displayPhoneNumber,
                ReceivedAt = receivedAt,
                ExtensionData = raw,
                MessageId = wire.Id,
                From = wire.From,
                Timestamp = timestamp,
                Context = context,
                Video = new WhatsAppInboundMedia
                {
                    MediaId = video.Id,
                    MimeType = video.MimeType,
                    Sha256Hash = video.Sha256,
                    Caption = video.Caption,
                },
            };
        }

        if (type == "sticker" && wire.Sticker is { Id: not null } sticker)
        {
            return new WhatsAppStickerMessageEvent
            {
                WhatsAppBusinessAccountId = wabaId,
                PhoneNumberId = phoneNumberId,
                DisplayPhoneNumber = displayPhoneNumber,
                ReceivedAt = receivedAt,
                ExtensionData = raw,
                MessageId = wire.Id,
                From = wire.From,
                Timestamp = timestamp,
                Context = context,
                Sticker = new WhatsAppInboundMedia
                {
                    MediaId = sticker.Id,
                    MimeType = sticker.MimeType,
                    Sha256Hash = sticker.Sha256,
                },
                IsAnimated = sticker.Animated ?? false,
            };
        }

        if (type == "location" && wire.Location is { Latitude: not null, Longitude: not null } location)
        {
            return new WhatsAppLocationMessageEvent
            {
                WhatsAppBusinessAccountId = wabaId,
                PhoneNumberId = phoneNumberId,
                DisplayPhoneNumber = displayPhoneNumber,
                ReceivedAt = receivedAt,
                ExtensionData = raw,
                MessageId = wire.Id,
                From = wire.From,
                Timestamp = timestamp,
                Context = context,
                Latitude = location.Latitude.Value,
                Longitude = location.Longitude.Value,
                Name = location.Name,
                Address = location.Address,
            };
        }

        if (type == "contacts" && wire.Contacts is { Count: > 0 } contactCards)
        {
            return new WhatsAppContactMessageEvent
            {
                WhatsAppBusinessAccountId = wabaId,
                PhoneNumberId = phoneNumberId,
                DisplayPhoneNumber = displayPhoneNumber,
                ReceivedAt = receivedAt,
                ExtensionData = raw,
                MessageId = wire.Id,
                From = wire.From,
                Timestamp = timestamp,
                Context = context,
                Contacts = contactCards.Select(MapContactCard).ToList(),
            };
        }

        if (type == "interactive" && wire.Interactive is not null)
        {
            var detail = wire.Interactive.Type switch
            {
                "button_reply" => wire.Interactive.ButtonReply,
                "list_reply" => wire.Interactive.ListReply,
                _ => null,
            };

            if (detail is { Id: not null, Title: not null } && wire.Interactive.Type is not null)
            {
                return new WhatsAppInteractiveReplyMessageEvent
                {
                    WhatsAppBusinessAccountId = wabaId,
                    PhoneNumberId = phoneNumberId,
                    DisplayPhoneNumber = displayPhoneNumber,
                    ReceivedAt = receivedAt,
                    ExtensionData = raw,
                    MessageId = wire.Id,
                    From = wire.From,
                    Timestamp = timestamp,
                    Context = context,
                    InteractiveType = wire.Interactive.Type,
                    ReplyId = detail.Id,
                    ReplyTitle = detail.Title,
                    ReplyDescription = detail.Description,
                };
            }
        }

        if (type == "button" && wire.Button is { Text: not null } button)
        {
            return new WhatsAppButtonReplyMessageEvent
            {
                WhatsAppBusinessAccountId = wabaId,
                PhoneNumberId = phoneNumberId,
                DisplayPhoneNumber = displayPhoneNumber,
                ReceivedAt = receivedAt,
                ExtensionData = raw,
                MessageId = wire.Id,
                From = wire.From,
                Timestamp = timestamp,
                Context = context,
                ButtonText = button.Text,
                ButtonPayload = button.Payload,
            };
        }

        if (type == "reaction" && wire.Reaction is { MessageId: not null } reaction)
        {
            return new WhatsAppReactionMessageEvent
            {
                WhatsAppBusinessAccountId = wabaId,
                PhoneNumberId = phoneNumberId,
                DisplayPhoneNumber = displayPhoneNumber,
                ReceivedAt = receivedAt,
                ExtensionData = raw,
                MessageId = wire.Id,
                From = wire.From,
                Timestamp = timestamp,
                Context = context,
                ReactedToMessageId = reaction.MessageId,
                Emoji = string.IsNullOrEmpty(reaction.Emoji) ? null : reaction.Emoji,
            };
        }

        return new UnknownWhatsAppMessageEvent
        {
            WhatsAppBusinessAccountId = wabaId,
            PhoneNumberId = phoneNumberId,
            DisplayPhoneNumber = displayPhoneNumber,
            ReceivedAt = receivedAt,
            ExtensionData = raw,
            MessageId = wire.Id,
            From = wire.From,
            Timestamp = timestamp,
            Context = context,
            MessageType = string.IsNullOrEmpty(wire.Type) ? "unknown" : wire.Type,
            RawContent = raw,
        };
    }

    private static WhatsAppMessageStatusEvent? ParseStatus(
        JsonElement raw, string wabaId, string? phoneNumberId, string? displayPhoneNumber, DateTimeOffset receivedAt)
    {
        var wire = TryDeserialize<WebhookStatusWire>(raw);
        if (wire is null || string.IsNullOrEmpty(wire.Id) || string.IsNullOrEmpty(wire.RecipientId))
        {
            return null;
        }

        var timestamp = ParseUnixTimestamp(wire.Timestamp, receivedAt);

        if (string.Equals(wire.Status, "sent", StringComparison.OrdinalIgnoreCase))
        {
            return new WhatsAppMessageSentEvent
            {
                WhatsAppBusinessAccountId = wabaId,
                PhoneNumberId = phoneNumberId,
                DisplayPhoneNumber = displayPhoneNumber,
                ReceivedAt = receivedAt,
                ExtensionData = raw,
                MessageId = wire.Id,
                RecipientId = wire.RecipientId,
                Timestamp = timestamp,
            };
        }

        if (string.Equals(wire.Status, "delivered", StringComparison.OrdinalIgnoreCase))
        {
            return new WhatsAppMessageDeliveredEvent
            {
                WhatsAppBusinessAccountId = wabaId,
                PhoneNumberId = phoneNumberId,
                DisplayPhoneNumber = displayPhoneNumber,
                ReceivedAt = receivedAt,
                ExtensionData = raw,
                MessageId = wire.Id,
                RecipientId = wire.RecipientId,
                Timestamp = timestamp,
            };
        }

        if (string.Equals(wire.Status, "read", StringComparison.OrdinalIgnoreCase))
        {
            return new WhatsAppMessageReadEvent
            {
                WhatsAppBusinessAccountId = wabaId,
                PhoneNumberId = phoneNumberId,
                DisplayPhoneNumber = displayPhoneNumber,
                ReceivedAt = receivedAt,
                ExtensionData = raw,
                MessageId = wire.Id,
                RecipientId = wire.RecipientId,
                Timestamp = timestamp,
            };
        }

        if (string.Equals(wire.Status, "failed", StringComparison.OrdinalIgnoreCase))
        {
            return new WhatsAppMessageFailedEvent
            {
                WhatsAppBusinessAccountId = wabaId,
                PhoneNumberId = phoneNumberId,
                DisplayPhoneNumber = displayPhoneNumber,
                ReceivedAt = receivedAt,
                ExtensionData = raw,
                MessageId = wire.Id,
                RecipientId = wire.RecipientId,
                Timestamp = timestamp,
                Errors = wire.Errors?.Select(MapError).ToList() ?? [],
            };
        }

        return new UnknownWhatsAppMessageStatusEvent
        {
            WhatsAppBusinessAccountId = wabaId,
            PhoneNumberId = phoneNumberId,
            DisplayPhoneNumber = displayPhoneNumber,
            ReceivedAt = receivedAt,
            ExtensionData = raw,
            MessageId = wire.Id,
            RecipientId = wire.RecipientId,
            Timestamp = timestamp,
            Status = string.IsNullOrEmpty(wire.Status) ? "unknown" : wire.Status,
        };
    }

    private static WhatsAppInboundContactCard MapContactCard(WebhookMessageContactCardWire card) => new()
    {
        FormattedName = card.Name?.FormattedName,
        Phones = card.Phones?.Select(phone => new WhatsAppInboundContactPhone
        {
            Phone = phone.Phone,
            WaId = phone.WaId,
            Type = phone.Type,
        }).ToList() ?? [],
    };

    private static WhatsAppWebhookError MapError(WebhookErrorWire error) => new()
    {
        Code = error.Code,
        Title = error.Title,
        Message = error.Message,
        Details = error.ErrorData?.Details,
    };

    private static DateTimeOffset ParseUnixTimestamp(string? timestamp, DateTimeOffset fallback) =>
        long.TryParse(timestamp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : fallback;

    private static T? TryDeserialize<T>(JsonElement element)
        where T : class
    {
        try
        {
            return element.Deserialize<T>(WhatsAppWebhookJsonSerializerOptions.Default);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
