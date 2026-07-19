using WhatsApp.Core.AspNetCore.Options;

namespace WhatsApp.Core.AspNetCore.Internal;

/// <summary>
/// Shared rules for when webhook signature validation may be skipped.
/// </summary>
internal static class WhatsAppWebhookSignaturePolicy
{
    /// <summary>
    /// Returns <see langword="true"/> only when signature validation is explicitly disabled
    /// and the insecure opt-in flag is set.
    /// </summary>
    public static bool MaySkipSignatureValidation(WhatsAppWebhookOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return !options.RequireSignatureValidation && options.AllowInsecureNoSignatureValidation;
    }

    /// <summary>
    /// Throws if per-endpoint options disable signature validation without the insecure opt-in.
    /// </summary>
    public static void EnsureMappable(WhatsAppWebhookOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.RequireSignatureValidation && !options.AllowInsecureNoSignatureValidation)
        {
            throw new InvalidOperationException(
                $"{nameof(WhatsAppWebhookOptions.RequireSignatureValidation)} may only be set to false when "
                + $"{nameof(WhatsAppWebhookOptions.AllowInsecureNoSignatureValidation)} is also true "
                + "(local development and tests only; must never be enabled in production).");
        }

        if (options.MaxRequestBodyBytes <= 0)
        {
            throw new InvalidOperationException(
                $"{nameof(WhatsAppWebhookOptions.MaxRequestBodyBytes)} must be a positive number of bytes.");
        }

        if (options.MaxDegreeOfParallelism <= 0)
        {
            throw new InvalidOperationException(
                $"{nameof(WhatsAppWebhookOptions.MaxDegreeOfParallelism)} must be at least 1.");
        }

        if (MaySkipSignatureValidation(options) && IsProductionEnvironment())
        {
            throw new InvalidOperationException(
                "Disabling webhook signature validation is not allowed when ASPNETCORE_ENVIRONMENT or "
                + "DOTNET_ENVIRONMENT is Production.");
        }
    }

    public static bool IsProductionEnvironment()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        return string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);
    }
}
