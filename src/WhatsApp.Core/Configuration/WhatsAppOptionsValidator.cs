using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WhatsApp.Core.Diagnostics;

namespace WhatsApp.Core.Configuration;

/// <summary>
/// Validates a <see cref="WhatsAppOptions"/> instance when it is first resolved through
/// <see cref="IOptionsMonitor{TOptions}"/>, catching misconfiguration at startup (or on first
/// use) rather than deep inside a request pipeline.
/// </summary>
public sealed class WhatsAppOptionsValidator : IValidateOptions<WhatsAppOptions>
{
    private readonly ILogger<WhatsAppOptionsValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppOptionsValidator"/> class that
    /// discards insecure-configuration warnings (useful in unit tests).
    /// </summary>
    public WhatsAppOptionsValidator()
        : this(NullLogger<WhatsAppOptionsValidator>.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppOptionsValidator"/> class.
    /// </summary>
    /// <param name="logger">The logger used to emit insecure-configuration warnings.</param>
    public WhatsAppOptionsValidator(ILogger<WhatsAppOptionsValidator> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, WhatsAppOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var accountLabel = string.IsNullOrEmpty(name) ? WhatsAppOptions.DefaultAccountName : name;
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.PhoneNumberId))
        {
            failures.Add($"{nameof(WhatsAppOptions.PhoneNumberId)} is required.");
        }

        if (string.IsNullOrWhiteSpace(options.AccessToken))
        {
            failures.Add($"{nameof(WhatsAppOptions.AccessToken)} is required.");
        }

        if (string.IsNullOrWhiteSpace(options.GraphApiVersion))
        {
            failures.Add($"{nameof(WhatsAppOptions.GraphApiVersion)} is required.");
        }
        else if (string.Equals(options.GraphApiVersion, "latest", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add(
                $"{nameof(WhatsAppOptions.GraphApiVersion)} must be a specific pinned version (e.g. \"v21.0\"), not \"latest\".");
        }

        if (options.BaseAddress is null)
        {
            failures.Add($"{nameof(WhatsAppOptions.BaseAddress)} is required.");
        }
        else if (!options.AllowInsecureHttp
            && !string.Equals(options.BaseAddress.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            failures.Add(
                $"{nameof(WhatsAppOptions.BaseAddress)} must use HTTPS unless {nameof(WhatsAppOptions.AllowInsecureHttp)} "
                + "is set to true (test-only; must never be enabled in production).");
        }
        else if (options.AllowInsecureHttp)
        {
            if (IsProductionEnvironment())
            {
                failures.Add(
                    $"{nameof(WhatsAppOptions.AllowInsecureHttp)} must not be enabled when ASPNETCORE_ENVIRONMENT or "
                    + "DOTNET_ENVIRONMENT is Production.");
            }
            else
            {
                WhatsAppLog.InsecureHttpAllowed(_logger, accountLabel);
            }
        }

        if (options.Timeout <= TimeSpan.Zero)
        {
            failures.Add($"{nameof(WhatsAppOptions.Timeout)} must be a positive duration.");
        }

        if (options.Resilience is null)
        {
            failures.Add($"{nameof(WhatsAppOptions.Resilience)} must not be null.");
        }
        else if (options.Resilience.MaxSafeRetries < 0)
        {
            failures.Add(
                $"{nameof(WhatsAppResilienceOptions.MaxSafeRetries)} must not be negative.");
        }

        if (failures.Count == 0)
        {
            return ValidateOptionsResult.Success;
        }

        var prefixed = failures.ConvertAll(failure => $"[{accountLabel}] {failure}");
        return ValidateOptionsResult.Fail(prefixed);
    }

    private static bool IsProductionEnvironment()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        return string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);
    }
}
