namespace WhatsApp.Core.AspNetCore.Internal;

/// <summary>
/// Small, allocation-free hexadecimal encode/decode helpers used by webhook signature handling.
/// Kept independent of <see cref="System.Convert"/>'s hex APIs so behavior is identical across
/// every target framework this package supports.
/// </summary>
internal static class HexFormat
{
    private const string LowercaseAlphabet = "0123456789abcdef";

    /// <summary>
    /// Encodes <paramref name="bytes"/> as a lowercase hexadecimal string.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>A lowercase hexadecimal string twice the length of <paramref name="bytes"/>.</returns>
    public static string ToLowerHex(ReadOnlySpan<byte> bytes)
    {
        Span<char> chars = bytes.Length <= 64 ? stackalloc char[bytes.Length * 2] : new char[bytes.Length * 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            chars[i * 2] = LowercaseAlphabet[bytes[i] >> 4];
            chars[(i * 2) + 1] = LowercaseAlphabet[bytes[i] & 0x0F];
        }

        return new string(chars);
    }

    /// <summary>
    /// Attempts to decode a hexadecimal string into <paramref name="destination"/>, without
    /// throwing on malformed input.
    /// </summary>
    /// <param name="hex">The hexadecimal characters to decode.</param>
    /// <param name="destination">The buffer to decode into; must be exactly half the length of <paramref name="hex"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="hex"/> was valid hexadecimal of the expected length; otherwise <see langword="false"/>.</returns>
    public static bool TryDecode(ReadOnlySpan<char> hex, Span<byte> destination)
    {
        if (hex.Length != destination.Length * 2)
        {
            return false;
        }

        for (var i = 0; i < destination.Length; i++)
        {
            var hi = HexValue(hex[i * 2]);
            var lo = HexValue(hex[(i * 2) + 1]);
            if (hi < 0 || lo < 0)
            {
                return false;
            }

            destination[i] = (byte)((hi << 4) | lo);
        }

        return true;
    }

    private static int HexValue(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => -1,
    };
}
