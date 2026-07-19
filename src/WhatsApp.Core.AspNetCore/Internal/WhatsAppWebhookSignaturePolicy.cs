using WhatsApp.Core.AspNetCore.Options;
using WhatsApp.Core.Internal;

namespace WhatsApp.Core.AspNetCore.Internal;

/// <summary>
/// Shared rules for when webhook signature validation may be skipped.
/// </summary>
internal static class WhatsAppWebhookSignaturePolicy
{
    /// <summary>
    /// Returns <see langword="true"/> only when signature validation is explicitly disabled,
    /// the insecure opt-in flag is set, and the hosting environment is development-like.
    /// </summary>
    public static bool MaySkipSignatureValidation(WhatsAppWebhookOptions options, string? environmentName)
    {
        ArgumentNullException.ThrowIfNull(options);
        return !options.RequireSignatureValidation
            && options.AllowInsecureNoSignatureValidation
            && HostingEnvironmentNames.AllowsInsecureConfiguration(environmentName);
    }

    /// <summary>
    /// Throws if options are not safe to map onto a webhook endpoint.
    /// </summary>
    public static void EnsureMappable(WhatsAppWebhookOptions options, string? environmentName)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.RequireSignatureValidation && !options.AllowInsecureNoSignatureValidation)
        {
            throw new InvalidOperationException(
                $"{nameof(WhatsAppWebhookOptions.RequireSignatureValidation)} may only be set to false when "
                + $"{nameof(WhatsAppWebhookOptions.AllowInsecureNoSignatureValidation)} is also true "
                + "(Development/Testing only; must never be enabled outside those environments).");
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

        if (!options.RequireSignatureValidation
            && options.AllowInsecureNoSignatureValidation
            && HostingEnvironmentNames.RestrictsInsecureConfiguration(environmentName))
        {
            throw new InvalidOperationException(
                "Disabling webhook signature validation is only allowed when the hosting environment is "
                + "Development, Testing, or Test.");
        }
    }
}
