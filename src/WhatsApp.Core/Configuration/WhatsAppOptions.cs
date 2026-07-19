using System.Diagnostics;
using WhatsApp.Core.Internal;

namespace WhatsApp.Core.Configuration;

/// <summary>
/// Configuration for a single WhatsApp Business Account (a "named account" when multiple
/// numbers are configured in the same application).
/// </summary>
/// <remarks>
/// Instances are typically created and populated once at startup by the dependency injection
/// extensions in <c>WhatsApp.Core.DependencyInjection</c>, either from configuration or from a
/// delegate. <see cref="ToString"/> and the debugger display redact <see cref="AccessToken"/>,
/// <see cref="AppSecret"/>, and <see cref="VerifyToken"/> so that this type can be safely logged
/// or inspected without leaking credentials.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class WhatsAppOptions
{
    /// <summary>
    /// The name used to identify the default (unnamed) account when only a single WhatsApp
    /// account is configured.
    /// </summary>
    public const string DefaultAccountName = "Default";

    /// <summary>
    /// Gets or sets the WhatsApp Business phone number id that outbound messages are sent from
    /// and inbound webhooks are addressed to. This is a Meta-assigned identifier, not the phone
    /// number itself.
    /// </summary>
    public required string PhoneNumberId { get; set; }

    /// <summary>
    /// Gets or sets the bearer access token used to authenticate against the Graph API. Treated
    /// as a secret: never logged, never included in exception messages, and redacted from
    /// <see cref="ToString"/>.
    /// </summary>
    public required string AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the app secret used to validate inbound webhook signatures
    /// (<c>X-Hub-Signature-256</c>). Optional; only required when verifying webhooks. Treated as
    /// a secret and redacted from <see cref="ToString"/>.
    /// </summary>
    public string? AppSecret { get; set; }

    /// <summary>
    /// Gets or sets the verify token used to validate the webhook subscription handshake
    /// (<c>hub.verify_token</c>). Optional; only required when receiving webhooks. Treated as a
    /// secret and redacted from <see cref="ToString"/>.
    /// </summary>
    public string? VerifyToken { get; set; }

    /// <summary>
    /// Gets or sets the pinned Graph API version to use for all requests, e.g. <c>"v21.0"</c>.
    /// Must be an explicit version and must not be <c>"latest"</c>, so that Meta's periodic
    /// version deprecations cannot silently change request/response shapes underneath the
    /// application. Defaults to <see cref="GraphApiDefaults.DefaultGraphApiVersion"/>.
    /// </summary>
    public required string GraphApiVersion { get; set; } = GraphApiDefaults.DefaultGraphApiVersion;

    /// <summary>
    /// Gets or sets the base address of the Graph API. Defaults to
    /// <c>https://graph.facebook.com/</c>. Must use HTTPS unless <see cref="AllowInsecureHttp"/>
    /// is set, which exists solely to support pointing at a local test server.
    /// </summary>
    public Uri BaseAddress { get; set; } = new("https://graph.facebook.com/");

    /// <summary>
    /// Gets or sets the timeout applied to the underlying <see cref="System.Net.Http.HttpClient"/>.
    /// Defaults to 30 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets a value indicating whether a non-HTTPS <see cref="BaseAddress"/> is
    /// permitted. This exists only to support integration tests against a local HTTP test
    /// server and must never be enabled in production. Defaults to <see langword="false"/>.
    /// Enabling this emits a loud validation warning.
    /// </summary>
    public bool AllowInsecureHttp { get; set; }

    /// <summary>
    /// Gets additional host names (or leading-dot suffixes such as <c>.example.test</c>) that
    /// <c>DownloadMediaAsync</c> may follow beyond the built-in Meta CDN allowlist and
    /// <see cref="BaseAddress"/> host. Intended for tests that stub media downloads on a
    /// non-Meta host. Defaults to empty.
    /// </summary>
    public IList<string> AllowedMediaDownloadHosts { get; } = new List<string>();

    /// <summary>
    /// Gets or sets the resilience (safe-retry) settings applied to idempotent requests.
    /// </summary>
    public WhatsAppResilienceOptions Resilience { get; set; } = new();

    /// <summary>
    /// Returns a redacted, human-readable representation of these options, safe to log. Secrets
    /// (<see cref="AccessToken"/>, <see cref="AppSecret"/>, <see cref="VerifyToken"/>) are never
    /// included; <see cref="PhoneNumberId"/> is partially masked.
    /// </summary>
    /// <returns>A redacted string representation.</returns>
    public override string ToString() =>
        $"WhatsAppOptions {{ PhoneNumberId = {Redact.MaskTail(PhoneNumberId)}, "
        + $"AccessToken = {Redact.Secret(AccessToken)}, "
        + $"AppSecret = {Redact.Secret(AppSecret)}, "
        + $"VerifyToken = {Redact.Secret(VerifyToken)}, "
        + $"GraphApiVersion = {GraphApiVersion}, "
        + $"BaseAddress = {BaseAddress}, "
        + $"Timeout = {Timeout}, "
        + $"AllowInsecureHttp = {AllowInsecureHttp} }}";
}
