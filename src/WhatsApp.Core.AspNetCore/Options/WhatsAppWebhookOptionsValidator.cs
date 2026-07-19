using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsApp.Core.AspNetCore.Diagnostics;
using WhatsApp.Core.Internal;

namespace WhatsApp.Core.AspNetCore.Options;

/// <summary>
/// Validates a <see cref="WhatsAppWebhookOptions"/> instance when it is first resolved,
/// catching misconfiguration early and refusing insecure signature-validation opt-out unless
/// explicitly allowed in a development-like environment.
/// </summary>
public sealed class WhatsAppWebhookOptionsValidator : IValidateOptions<WhatsAppWebhookOptions>
{
    private readonly ILogger<WhatsAppWebhookOptionsValidator> _logger;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppWebhookOptionsValidator"/> class.
    /// </summary>
    /// <param name="logger">The logger used to emit the insecure-configuration warning.</param>
    /// <param name="environment">The current hosting environment.</param>
    public WhatsAppWebhookOptionsValidator(
        ILogger<WhatsAppWebhookOptionsValidator> logger,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(environment);
        _logger = logger;
        _environment = environment;
    }

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, WhatsAppWebhookOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var accountLabel = options.AccountName ?? WhatsApp.Core.Configuration.WhatsAppOptions.DefaultAccountName;
        var failures = new List<string>();

        if (options.MaxRequestBodyBytes <= 0)
        {
            failures.Add($"{nameof(WhatsAppWebhookOptions.MaxRequestBodyBytes)} must be a positive number of bytes.");
        }

        if (options.MaxDegreeOfParallelism <= 0)
        {
            failures.Add($"{nameof(WhatsAppWebhookOptions.MaxDegreeOfParallelism)} must be at least 1.");
        }

        if (!options.RequireSignatureValidation)
        {
            if (!options.AllowInsecureNoSignatureValidation)
            {
                failures.Add(
                    $"{nameof(WhatsAppWebhookOptions.RequireSignatureValidation)} may only be set to false when "
                    + $"{nameof(WhatsAppWebhookOptions.AllowInsecureNoSignatureValidation)} is also true "
                    + "(Development/Testing only).");
            }
            else if (HostingEnvironmentNames.RestrictsInsecureConfiguration(_environment.EnvironmentName))
            {
                failures.Add(
                    "Disabling webhook signature validation is only allowed when the hosting environment is "
                    + "Development, Testing, or Test.");
            }
            else
            {
                WhatsAppWebhookLog.SignatureValidationDisabled(_logger, accountLabel);
            }
        }

        if (failures.Count == 0)
        {
            return ValidateOptionsResult.Success;
        }

        var prefixed = failures.ConvertAll(failure => $"[{accountLabel}] {failure}");
        return ValidateOptionsResult.Fail(prefixed);
    }
}
