using System.Security.Cryptography;
using System.Text;

namespace WhatsApp.Core.Testing.Signatures;

/// <summary>
/// Helpers for generating Meta-compatible <c>X-Hub-Signature-256</c> values in tests.
/// </summary>
/// <remarks>
/// Uses the same HMAC-SHA256 algorithm as production validation in
/// <c>WhatsApp.Core.AspNetCore</c>: <c>sha256=</c> followed by lowercase hexadecimal.
/// </remarks>
public static class WhatsAppWebhookTestSignatures
{
    /// <summary>
    /// Creates a Meta-compatible <c>X-Hub-Signature-256</c> header value.
    /// </summary>
    /// <param name="payload">The exact raw request body.</param>
    /// <param name="appSecret">The Meta app secret.</param>
    /// <returns>A signature of the form <c>sha256=</c> followed by lowercase hex.</returns>
    public static string CreateSignature(ReadOnlySpan<byte> payload, string appSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appSecret);
        var key = Encoding.UTF8.GetBytes(appSecret);
        var hash = HMACSHA256.HashData(key, payload);
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}
