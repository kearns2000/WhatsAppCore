using System.Security.Cryptography;
using System.Text;
using WhatsApp.Core.AspNetCore.Internal;

namespace WhatsApp.Core.AspNetCore.Signature;

/// <summary>
/// The shared HMAC-SHA256 algorithm Meta uses to sign webhook deliveries via the
/// <c>X-Hub-Signature-256</c> header. Exposed as a public, side-effect-free utility so that
/// other packages (notably <c>WhatsApp.Core.Testing</c>, when generating fake webhook
/// deliveries) can compute the exact same signature that a real Meta webhook would carry,
/// without duplicating or drifting from the algorithm used by
/// <see cref="WhatsAppWebhookSignatureValidator"/>.
/// </summary>
public static class WhatsAppWebhookSignature
{
    /// <summary>
    /// The name of the HTTP header Meta uses to carry the webhook signature.
    /// </summary>
    public const string HeaderName = "X-Hub-Signature-256";

    /// <summary>
    /// The required prefix on the <see cref="HeaderName"/> value, identifying the digest
    /// algorithm as SHA-256.
    /// </summary>
    public const string SignaturePrefix = "sha256=";

    /// <summary>
    /// Computes the full <see cref="HeaderName"/> header value (including the
    /// <see cref="SignaturePrefix"/>) for <paramref name="payload"/>, signed with
    /// <paramref name="appSecret"/>.
    /// </summary>
    /// <param name="payload">The exact raw request body bytes to sign.</param>
    /// <param name="appSecret">The WhatsApp app secret shared with Meta.</param>
    /// <returns>A value of the form <c>sha256=&lt;lowercase-hex-digest&gt;</c>.</returns>
    public static string Compute(ReadOnlySpan<byte> payload, string appSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appSecret);

        Span<byte> digest = stackalloc byte[HMACSHA256.HashSizeInBytes];
        var secretBytes = Encoding.UTF8.GetBytes(appSecret);
        HMACSHA256.HashData(secretBytes, payload, digest);
        return SignaturePrefix + HexFormat.ToLowerHex(digest);
    }

    /// <summary>
    /// Determines whether <paramref name="suppliedSignature"/> is a valid HMAC-SHA256 signature
    /// of <paramref name="payload"/> under <paramref name="appSecret"/>.
    /// </summary>
    /// <param name="payload">The exact raw request body bytes that were (allegedly) signed.</param>
    /// <param name="appSecret">The WhatsApp app secret shared with Meta.</param>
    /// <param name="suppliedSignature">The value of the <see cref="HeaderName"/> header, including the <see cref="SignaturePrefix"/>.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="suppliedSignature"/> uses the
    /// <see cref="SignaturePrefix"/> and its digest matches; otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Comparison is performed in constant time via
    /// <see cref="CryptographicOperations.FixedTimeEquals"/> to avoid leaking timing
    /// information that could assist an attacker in forging a signature.
    /// </remarks>
    public static bool IsValid(ReadOnlySpan<byte> payload, string appSecret, string suppliedSignature)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appSecret);

        if (string.IsNullOrEmpty(suppliedSignature)
            || !suppliedSignature.AsSpan().StartsWith(SignaturePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var suppliedHex = suppliedSignature.AsSpan(SignaturePrefix.Length);

        Span<byte> supplied = stackalloc byte[HMACSHA256.HashSizeInBytes];
        if (!HexFormat.TryDecode(suppliedHex, supplied))
        {
            return false;
        }

        Span<byte> expected = stackalloc byte[HMACSHA256.HashSizeInBytes];
        var secretBytes = Encoding.UTF8.GetBytes(appSecret);
        HMACSHA256.HashData(secretBytes, payload, expected);

        return CryptographicOperations.FixedTimeEquals(expected, supplied);
    }
}
