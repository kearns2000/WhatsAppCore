using System.Security.Cryptography;
using System.Text;

namespace WhatsApp.Core.AspNetCore.Internal;

/// <summary>
/// A constant-time string equality check for comparing secrets such as webhook verify tokens,
/// to avoid leaking their value (including length) through response-timing side channels.
/// </summary>
internal static class ConstantTimeEquals
{
    /// <summary>
    /// Compares two strings for equality in constant time with respect to both content and
    /// length by hashing each UTF-8 encoding before a fixed-time digest comparison.
    /// </summary>
    /// <param name="a">The first string.</param>
    /// <param name="b">The second string.</param>
    /// <returns><see langword="true"/> if both strings are non-null, non-empty, and equal; otherwise <see langword="false"/>.</returns>
    public static bool StringsEqual(string? a, string? b)
    {
        // Always hash both sides (null treated as empty) so timing does not leak null vs non-null.
        var aHash = SHA256.HashData(Encoding.UTF8.GetBytes(a ?? string.Empty));
        var bHash = SHA256.HashData(Encoding.UTF8.GetBytes(b ?? string.Empty));
        var digestsEqual = CryptographicOperations.FixedTimeEquals(aHash, bHash);

        // Empty or null verify tokens are never valid.
        return digestsEqual && !string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b);
    }
}
