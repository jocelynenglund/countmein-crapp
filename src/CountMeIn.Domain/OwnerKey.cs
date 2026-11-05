using System.Security.Cryptography;
using System.Text;

namespace CountMeIn.Domain;

/// <summary>
/// The secret that proves you started a check. Only the SHA-256 hash is ever stored,
/// so a leaked database does not leak the keys.
/// </summary>
public static class OwnerKey
{
    /// <summary>A fresh key: 32 url-safe characters from 24 random bytes.</summary>
    public static string Generate() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)).Replace('+', '-').Replace('/', '_');

    public static string Hash(string key) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))).ToLowerInvariant();

    /// <summary>Constant-time comparison; a missing key never matches.</summary>
    public static bool Matches(string? key, string hash)
    {
        if (string.IsNullOrEmpty(key)) return false;
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(Hash(key)), Encoding.UTF8.GetBytes(hash));
    }
}
