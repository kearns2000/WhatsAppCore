using System.Text.Json;
using System.Text.Json.Serialization;

namespace WhatsApp.Core.Serialization;

/// <summary>
/// Provides the shared <see cref="JsonSerializerOptions"/> instance used to (de)serialize the
/// Graph API wire-format DTOs defined in <see cref="Wire"/>, backed by the source-generated
/// <see cref="WhatsAppJsonSerializerContext"/>.
/// </summary>
internal static class WhatsAppJsonSerializerOptions
{
    /// <summary>
    /// Gets the shared, immutable <see cref="JsonSerializerOptions"/> instance.
    /// </summary>
    public static JsonSerializerOptions Default { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = WhatsAppJsonSerializerContext.Default,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };
        options.MakeReadOnly();
        return options;
    }
}
