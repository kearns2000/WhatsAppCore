using System.Security.Cryptography;
using System.Text;

namespace WhatsApp.Core.AspNetCore.Internal;

/// <summary>
/// A constant-time string equality check for comparing secrets such as webhook verify tokens,
/// to avoid leaking their value (including length) through response-timing side channels.
/// </summary>
internal static class ConstantTimeEquals
{
    private const int MaxTokenUtf8Bytes = 256;

    /// <summary>
    /// Compares two strings for equality in constant time by UTF-8-encoding both into fixed-size
    /// buffers and using <see cref="CryptographicOperations.FixedTimeEquals"/>.
    /// </summary>
    /// <param name="a">The first string.</param>
    /// <param name="b">The second string.</param>
    /// <returns><see langword="true"/> if both strings are non-null, non-whitespace, within the
    /// max UTF-8 length, and equal; otherwise <see langword="false"/>.</returns>
    public static bool StringsEqual(string? a, string? b)
    {
        Span<byte> aBuffer = stackalloc byte[MaxTokenUtf8Bytes];
        Span<byte> bBuffer = stackalloc byte[MaxTokenUtf8Bytes];
        aBuffer.Clear();
        bBuffer.Clear();

        var aOk = TryEncodeFixed(a, aBuffer);
        var bOk = TryEncodeFixed(b, bBuffer);
        var equal = CryptographicOperations.FixedTimeEquals(aBuffer, bBuffer);

        return equal && aOk && bOk;
    }

    private static bool TryEncodeFixed(string? value, Span<byte> destination)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount is 0 or > MaxTokenUtf8Bytes)
        {
            return false;
        }

        Encoding.UTF8.GetBytes(value, destination);
        return true;
    }
}
