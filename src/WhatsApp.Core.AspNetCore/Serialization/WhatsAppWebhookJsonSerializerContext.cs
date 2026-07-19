using System.Text.Json.Serialization;
using WhatsApp.Core.AspNetCore.Serialization.Wire;

namespace WhatsApp.Core.AspNetCore.Serialization;

/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> for the wire-format DTOs used to
/// deserialize inbound webhook payloads.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(WebhookEnvelopeWire))]
[JsonSerializable(typeof(WebhookEntryWire))]
[JsonSerializable(typeof(WebhookChangeWire))]
[JsonSerializable(typeof(WebhookValueWire))]
[JsonSerializable(typeof(WebhookMetadataWire))]
[JsonSerializable(typeof(WebhookContactWire))]
[JsonSerializable(typeof(WebhookErrorWire))]
[JsonSerializable(typeof(WebhookMessageWire))]
[JsonSerializable(typeof(WebhookStatusWire))]
[JsonSerializable(typeof(WebhookInteractiveContentWire))]
[JsonSerializable(typeof(WebhookMessageContactCardWire))]
internal sealed partial class WhatsAppWebhookJsonSerializerContext : JsonSerializerContext;
