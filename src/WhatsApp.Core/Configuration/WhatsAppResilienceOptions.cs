namespace WhatsApp.Core.Configuration;

/// <summary>
/// Controls opt-in, narrowly-scoped retry behavior for idempotent WhatsApp Cloud API calls.
/// </summary>
/// <remarks>
/// Sending a message is never automatically retried by this library, because a POST to the
/// <c>/messages</c> endpoint is not guaranteed to be idempotent (a retried send could result in a
/// duplicate message being delivered to the end user). These options only affect read-only or
/// idempotent operations such as fetching media metadata or deleting media.
/// </remarks>
public sealed class WhatsAppResilienceOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether GET requests for media metadata and DELETE
    /// requests for media may be retried a limited number of times on transient failures (HTTP
    /// 408/429/5xx). Message sends and media uploads are never affected by this setting.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool EnableSafeRetries { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of additional attempts made after the initial request
    /// when <see cref="EnableSafeRetries"/> is <see langword="true"/>. Defaults to 2.
    /// </summary>
    public int MaxSafeRetries { get; set; } = 2;
}
