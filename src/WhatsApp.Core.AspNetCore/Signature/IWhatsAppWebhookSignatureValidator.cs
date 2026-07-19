namespace WhatsApp.Core.AspNetCore.Signature;

/// <summary>
/// Validates that an inbound WhatsApp webhook payload was sent by Meta and has not been
/// tampered with in transit, by checking the <c>X-Hub-Signature-256</c> header against an
/// HMAC-SHA256 digest of the exact raw request body.
/// </summary>
/// <remarks>
/// Implementations must compare signatures in constant time (see
/// <see cref="System.Security.Cryptography.CryptographicOperations.FixedTimeEquals"/>) and must
/// never log the supplied signature, the computed signature, or the app secret used to compute
/// it, since doing so could allow an attacker to forge valid signatures.
/// </remarks>
public interface IWhatsAppWebhookSignatureValidator
{
    /// <summary>
    /// Determines whether <paramref name="suppliedSignature"/> is a valid HMAC-SHA256 signature
    /// of <paramref name="payload"/>.
    /// </summary>
    /// <param name="payload">The exact raw request body bytes, unmodified and undecoded.</param>
    /// <param name="suppliedSignature">
    /// The value of the <c>X-Hub-Signature-256</c> header, including the required
    /// <c>sha256=</c> prefix.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="suppliedSignature"/> is present, uses the
    /// <c>sha256=</c> prefix, and matches the HMAC-SHA256 digest of <paramref name="payload"/>
    /// computed with the configured app secret; otherwise <see langword="false"/>.
    /// </returns>
    bool IsValid(ReadOnlySpan<byte> payload, string suppliedSignature);
}
