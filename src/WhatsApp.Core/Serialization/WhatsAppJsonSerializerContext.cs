using System.Text.Json.Serialization;
using WhatsApp.Core.Responses;
using WhatsApp.Core.Serialization.Wire;

namespace WhatsApp.Core.Serialization;

/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> for the small set of wire-format DTOs
/// used to deserialize Graph API responses and errors. Outbound message payloads are built
/// directly as <see cref="System.Text.Json.Nodes.JsonObject"/> graphs (see
/// <see cref="Messages.WhatsAppMessageRequest"/>) rather than through this context, since their
/// shapes vary per message type and rely on explicit <see cref="JsonPropertyNameAttribute"/>
/// values rather than a naming policy.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(SendMessageWireResponse))]
[JsonSerializable(typeof(MediaUploadWireResponse))]
[JsonSerializable(typeof(MediaMetadataWireResponse))]
[JsonSerializable(typeof(GraphErrorWireResponse))]
[JsonSerializable(typeof(WhatsAppMessageId))]
[JsonSerializable(typeof(WhatsAppResponseContact))]
internal sealed partial class WhatsAppJsonSerializerContext : JsonSerializerContext;
