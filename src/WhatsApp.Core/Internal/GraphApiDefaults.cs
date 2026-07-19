namespace WhatsApp.Core.Internal;

/// <summary>
/// Central location for default values related to the Meta Graph API. This is the single
/// place in the library where the default Graph API version string is defined; it must never
/// be duplicated elsewhere.
/// </summary>
internal static class GraphApiDefaults
{
    /// <summary>
    /// The default Graph API version used when no explicit version is configured.
    /// </summary>
    public const string DefaultGraphApiVersion = "v21.0";
}
