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
        if (a is null || b is null)
        {
            return false;
        }

        var aHash = SHA256.HashData(Encoding.UTF8.GetBytes(a));
        var bHash = SHA256.HashData(Encoding.UTF8.GetBytes(b));
        var digestsEqual = CryptographicOperations.FixedTimeEquals(aHash, bHash);

        // Empty verify tokens are never valid, even if both sides are empty.
        return digestsEqual && a.Length > 0 && b.Length > 0;
    }
}
