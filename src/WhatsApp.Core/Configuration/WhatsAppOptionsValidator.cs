using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WhatsApp.Core.Diagnostics;
using WhatsApp.Core.Internal;

namespace WhatsApp.Core.Configuration;

/// <summary>
/// Validates a <see cref="WhatsAppOptions"/> instance when it is first resolved through
/// <see cref="IOptionsMonitor{TOptions}"/>, catching misconfiguration at startup (or on first
/// use) rather than deep inside a request pipeline.
/// </summary>
public sealed class WhatsAppOptionsValidator : IValidateOptions<WhatsAppOptions>
{
    private readonly ILogger<WhatsAppOptionsValidator> _logger;
    private readonly string? _environmentName;

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
    /// Falls back to ASPNETCORE_ENVIRONMENT / DOTNET_ENVIRONMENT when no host environment is available.
    /// </summary>
    /// <param name="logger">The logger used to emit insecure-configuration warnings.</param>
    public WhatsAppOptionsValidator(ILogger<WhatsAppOptionsValidator> logger)
        : this(logger, environmentName: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppOptionsValidator"/> class that prefers
    /// <see cref="IHostEnvironment.EnvironmentName"/> for insecure-configuration gates (same source
    /// as webhook options validation).
    /// </summary>
    /// <param name="logger">The logger used to emit insecure-configuration warnings.</param>
    /// <param name="environment">The current hosting environment.</param>
    public WhatsAppOptionsValidator(ILogger<WhatsAppOptionsValidator> logger, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(environment);
        _logger = logger;
        _environmentName = environment.EnvironmentName;
    }

    private WhatsAppOptionsValidator(ILogger<WhatsAppOptionsValidator> logger, string? environmentName)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _environmentName = environmentName;
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
            if (HostingEnvironmentNames.RestrictsInsecureConfiguration(_environmentName))
            {
                failures.Add(
                    $"{nameof(WhatsAppOptions.AllowInsecureHttp)} is only allowed when the hosting environment is "
                    + "Development, Testing, or Test.");
            }
            else
            {
                WhatsAppLog.InsecureHttpAllowed(_logger, accountLabel);
            }
        }

        foreach (var host in options.AllowedMediaDownloadHosts)
        {
            if (!MediaDownloadUrlValidator.IsValidAllowedHostEntry(host, out var hostError))
            {
                failures.Add(hostError);
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
}
