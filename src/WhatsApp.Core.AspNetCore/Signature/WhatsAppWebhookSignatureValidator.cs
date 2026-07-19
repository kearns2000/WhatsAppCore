namespace WhatsApp.Core.AspNetCore.Signature;

/// <summary>
/// Default <see cref="IWhatsAppWebhookSignatureValidator"/> implementation, backed by the
/// <see cref="WhatsAppWebhookSignature"/> shared algorithm.
/// </summary>
public sealed class WhatsAppWebhookSignatureValidator : IWhatsAppWebhookSignatureValidator
{
    private readonly string _appSecret;

    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppWebhookSignatureValidator"/> class.
    /// </summary>
    /// <param name="appSecret">The Meta app secret used as the HMAC key.</param>
    public WhatsAppWebhookSignatureValidator(string appSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appSecret);
        _appSecret = appSecret;
    }

    /// <inheritdoc />
    public bool IsValid(ReadOnlySpan<byte> payload, string suppliedSignature) =>
        WhatsAppWebhookSignature.IsValid(payload, _appSecret, suppliedSignature);
}
