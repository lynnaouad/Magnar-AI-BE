using System.Security.Cryptography;
using System.Text;

namespace Magnar.AI.Application.Helpers;

public static class ApiKeyHashing
{
    /// <summary>
    /// Computes an HMAC-SHA256 hash for an API key or secret.
    /// 
    /// - Uses <paramref name="serverSecret"/> as the HMAC key (known only to the server).
    /// - Builds a payload string in the form "<publicId>:<secretPart>".
    /// - Hashes the payload with HMAC-SHA256 and returns it as a hex string.
    /// 
    /// Purpose: Verifies that a given <publicId>/<secretPart> pair is valid without
    /// storing the original secret directly in the database.
    /// </summary>
    public static string ComputeHash(string serverSecret, string publicId, string secretPart)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(serverSecret));
       
        var payload = $"{publicId}:{secretPart}";
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Compares two strings in constant time to prevent timing attacks.
    /// 
    /// - If lengths differ, immediately returns false.
    /// - Otherwise, XORs each character and accumulates the differences.
    /// - Returns true only if all characters match.
    /// 
    /// Purpose: Protects against attackers measuring string comparison timing to guess
    /// secrets (e.g., API keys, HMAC signatures, or passwords).
    /// </summary>
    public static bool ConstantTimeEquals(string a, string b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        var diff = 0;
        for (int i = 0; i < a.Length; i++)
        {
            diff |= a[i] ^ b[i];
        }
        
        return diff == 0;
    }

    /// <summary>
    /// Encodes a byte array into a Base32 string using the RFC 4648 alphabet
    /// (A–Z and 2–7).
    /// 
    /// - Takes 5 bits at a time from the input bytes.
    /// - Maps each 5-bit group to a character in the Base32 alphabet.
    /// - Pads the last group if there are leftover bits.
    /// 
    /// Produces a case-insensitive, URL-safe, human-friendly encoding,
    /// often used for public IDs, tokens, or secret keys.
    /// </summary>
    public static string Base32(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bits = 0;
        var value = 0;
        var output = new StringBuilder();

        foreach (var b in data)
        {
            value = (value << 8) | b;
            bits += 8;

            while (bits >= 5)
            {
                output.Append(alphabet[(value >> (bits - 5)) & 31]);
                bits -= 5;
            }
        }

        if (bits > 0)
        {
            output.Append(alphabet[(value << (5 - bits)) & 31]);
        }

        return output.ToString();
    }

    /// <summary>
    /// Encodes a byte array into a Base64Url string.
    /// 
    /// - Standard Base64 uses '+', '/', and '=' which are not URL-safe.
    /// - This version:
    ///   * Removes trailing '=' padding.
    ///   * Replaces '+' with '-'.
    ///   * Replaces '/' with '_'.
    /// 
    /// Result: URL- and filename-safe Base64 encoding (RFC 4648 §5).
    /// Commonly used in JWTs, OAuth tokens, and query-safe identifiers.
    /// </summary>
    public static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    /// <summary>
    /// Decodes a Base64Url string back into bytes.
    /// 
    /// - Reverses URL-safe replacements ('-' → '+', '_' → '/').
    /// — Pads with '=' so length is a multiple of 4 (required by Base64).
    /// - Decodes using the normal Base64 decoder.
    /// 
    /// Accepts strings encoded with <see cref="Base64UrlEncode"/>.
    /// </summary>
    public static byte[] Base64UrlDecode(string input)
    {
        var pad = input.Replace('-', '+').Replace('_', '/');
        switch (pad.Length % 4)
        {
            case 2: pad += "=="; break;
            case 3: pad += "="; break;
        }
        return Convert.FromBase64String(pad);
    }
}