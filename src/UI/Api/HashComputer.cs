using System.Security.Cryptography;
using System.Text;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Computes lowercase hex digests for UTF-8 text using SHA-256, MD5, and SHA-1.
/// </summary>
public static class HashComputer
{
    /// <summary>
    /// Returns lowercase hex SHA-256, MD5, and SHA-1 hashes of <paramref name="text"/> encoded as UTF-8.
    /// </summary>
    public static HashResponse Compute(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes));
        var md5 = Convert.ToHexStringLower(MD5.HashData(bytes));
        var sha1 = Convert.ToHexStringLower(SHA1.HashData(bytes));
        return new HashResponse(sha256, md5, sha1);
    }
}
