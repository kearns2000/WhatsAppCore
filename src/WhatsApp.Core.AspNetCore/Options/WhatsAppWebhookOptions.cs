namespace WhatsApp.Core.AspNetCore.Options;

/// <summary>
/// Configures how an inbound WhatsApp webhook endpoint mapped with
/// <c>MapWhatsAppWebhook</c> verifies, validates, and dispatches deliveries.
/// </summary>
public sealed class WhatsAppWebhookOptions
{
    /// <summary>
    /// The default maximum request body size accepted from a webhook delivery (1 MiB), chosen to
    /// comfortably exceed the largest payloads Meta sends while still bounding memory use.
    /// </summary>
    public const long DefaultMaxRequestBodyBytes = 1_048_576;

    /// <summary>
    /// The default maximum number of events dispatched concurrently when
    /// <see cref="DispatchMode"/> is <see cref="WhatsAppWebhookDispatchMode.Parallel"/>.
    /// </summary>
    public const int DefaultMaxDegreeOfParallelism = 4;

    /// <summary>
    /// Gets or sets the name of the WhatsApp account (as registered with <c>AddWhatsAppCore</c>)
    /// whose <c>VerifyToken</c> and <c>AppSecret</c> should be used for this webhook endpoint.
    /// <see langword="null"/> selects the default account.
    /// </summary>
    public string? AccountName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether inbound POST deliveries must carry a valid
    /// <c>X-Hub-Signature-256</c> header. Defaults to <see langword="true"/>. Disable only for
    /// local development and automated tests, and only together with
    /// <see cref="AllowInsecureNoSignatureValidation"/>.
    /// </summary>
    public bool RequireSignatureValidation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="RequireSignatureValidation"/> may be
    /// set to <see langword="false"/>. Defaults to <see langword="false"/>. Must be set
    /// explicitly for local development and tests; production must leave both flags at their
    /// secure defaults. Enabling this emits a loud validation warning.
    /// </summary>
    public bool AllowInsecureNoSignatureValidation { get; set; }

    /// <summary>
    /// Gets or sets the maximum accepted request body size, in bytes. Requests whose body exceeds
    /// this limit are rejected with <c>413 Payload Too Large</c> before the body is fully buffered.
    /// Defaults to <see cref="DefaultMaxRequestBodyBytes"/> (1 MiB).
    /// </summary>
    public long MaxRequestBodyBytes { get; set; } = DefaultMaxRequestBodyBytes;

    /// <summary>
    /// Gets or sets how parsed webhook events are dispatched to registered handlers. Defaults to
    /// <see cref="WhatsAppWebhookDispatchMode.Sequential"/>.
    /// </summary>
    public WhatsAppWebhookDispatchMode DispatchMode { get; set; } = WhatsAppWebhookDispatchMode.Sequential;

    /// <summary>
    /// Gets or sets the maximum number of events dispatched concurrently when
    /// <see cref="DispatchMode"/> is <see cref="WhatsAppWebhookDispatchMode.Parallel"/>. Ignored
    /// when <see cref="DispatchMode"/> is <see cref="WhatsAppWebhookDispatchMode.Sequential"/>.
    /// Defaults to <see cref="DefaultMaxDegreeOfParallelism"/>.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = DefaultMaxDegreeOfParallelism;

    /// <summary>
    /// Creates a shallow copy of these options, used internally so that per-endpoint overrides
    /// supplied to <c>MapWhatsAppWebhook</c> never mutate shared, dependency-injected options.
    /// </summary>
    /// <returns>A new <see cref="WhatsAppWebhookOptions"/> with the same property values.</returns>
    public WhatsAppWebhookOptions Clone() => new()
    {
        AccountName = AccountName,
        RequireSignatureValidation = RequireSignatureValidation,
        AllowInsecureNoSignatureValidation = AllowInsecureNoSignatureValidation,
        MaxRequestBodyBytes = MaxRequestBodyBytes,
        DispatchMode = DispatchMode,
        MaxDegreeOfParallelism = MaxDegreeOfParallelism,
    };

    /// <summary>
    /// Returns a human-readable representation of these options, safe to log (they contain no secrets).
    /// </summary>
    public override string ToString() =>
        $"WhatsAppWebhookOptions {{ AccountName = {AccountName ?? "(default)"}, "
        + $"RequireSignatureValidation = {RequireSignatureValidation}, "
        + $"AllowInsecureNoSignatureValidation = {AllowInsecureNoSignatureValidation}, "
        + $"MaxRequestBodyBytes = {MaxRequestBodyBytes}, "
        + $"DispatchMode = {DispatchMode}, "
        + $"MaxDegreeOfParallelism = {MaxDegreeOfParallelism} }}";
}
