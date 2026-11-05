using System.Security.Cryptography;

namespace CountMeIn.Domain;

/// <summary>Ids travel in URLs, so they are limited to url-safe characters.</summary>
public static class Identifiers
{
    public const int MaxLength = 64;

    /// <summary>A short random id for a share link.</summary>
    public static string NewShareId() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(9)).Replace('+', '-').Replace('/', '_');

    /// <summary>Letters, digits, '-' and '_', at most 64 characters.</summary>
    public static bool IsValid(string? id) =>
        !string.IsNullOrEmpty(id) && id.Length <= MaxLength &&
        id.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
