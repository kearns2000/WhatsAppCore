using System.Text.Json;
using System.Text.Json.Serialization;

namespace WhatsApp.Core.AspNetCore.Serialization;

/// <summary>
/// Provides the shared <see cref="JsonSerializerOptions"/> instance used to deserialize inbound
/// webhook payloads, backed by the source-generated
/// <see cref="WhatsAppWebhookJsonSerializerContext"/>.
/// </summary>
internal static class WhatsAppWebhookJsonSerializerOptions
{
    /// <summary>
    /// Gets the shared, immutable <see cref="JsonSerializerOptions"/> instance.
    /// </summary>
    public static JsonSerializerOptions Default { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = WhatsAppWebhookJsonSerializerContext.Default,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };
        options.MakeReadOnly();
        return options;
    }
}
