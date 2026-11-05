using System.Security.Cryptography;
using System.Text;

namespace CountMeIn.Application;

/// <summary>Generates share ids and owner keys, and hashes keys so only the hash is stored.</summary>
public static class OwnerKeys
{
    private const string Alphabet = "abcdefghijkmnpqrstuvwxyz23456789";

    public static string NewShareId() => RandomNumberGenerator.GetString(Alphabet, 10);

    public static string NewOwnerKey() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();

    public static string Hash(string? ownerKey) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ownerKey ?? ""))).ToLowerInvariant();
}
