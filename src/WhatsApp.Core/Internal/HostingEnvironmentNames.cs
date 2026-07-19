namespace WhatsApp.Core.Internal;

/// <summary>
/// Classifies the process hosting environment for insecure-configuration gates.
/// Insecure flags are allowed only in Development/Testing; all other names (including unset)
/// are treated as restricted.
/// </summary>
internal static class HostingEnvironmentNames
{
    /// <summary>
    /// Returns <see langword="true"/> when the hosting environment is an explicit
    /// development-like name where insecure test-only settings may be used.
    /// </summary>
    /// <param name="environmentName">
    /// Optional environment name (e.g. from <c>IHostEnvironment.EnvironmentName</c>).
    /// When omitted, ASPNETCORE_ENVIRONMENT / DOTNET_ENVIRONMENT are consulted.
    /// </param>
    public static bool AllowsInsecureConfiguration(string? environmentName = null)
    {
        var environment = environmentName ?? GetEnvironmentName();
        return string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase)
            || string.Equals(environment, "Testing", StringComparison.OrdinalIgnoreCase)
            || string.Equals(environment, "Test", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns <see langword="true"/> when insecure configuration must be rejected.
    /// </summary>
    /// <param name="environmentName">
    /// Optional environment name override; see <see cref="AllowsInsecureConfiguration"/>.
    /// </param>
    public static bool RestrictsInsecureConfiguration(string? environmentName = null) =>
        !AllowsInsecureConfiguration(environmentName);

    public static string? GetEnvironmentName() =>
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
}
