namespace WhatsApp.Core.Internal;

/// <summary>
/// Helpers for producing safe-to-log representations of sensitive configuration values.
/// </summary>
internal static class Redact
{
    /// <summary>
    /// Fully redacts a secret value, revealing only whether it was set.
    /// </summary>
    /// <param name="value">The secret value.</param>
    /// <returns><c>"(not set)"</c> when null or empty; otherwise <c>"***"</c>.</returns>
    public static string Secret(string? value) => string.IsNullOrEmpty(value) ? "(not set)" : "***";

    /// <summary>
    /// Masks all but the last four characters of a value that is sensitive but occasionally
    /// useful to partially see for debugging (e.g. to distinguish between configured accounts).
    /// </summary>
    /// <param name="value">The value to mask.</param>
    /// <returns>A masked representation such as <c>"***1234"</c>.</returns>
    public static string MaskTail(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "(not set)";
        }

        return value.Length <= 4 ? "***" : $"***{value[^4..]}";
    }
}
