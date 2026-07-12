using System.Security.Cryptography;
using System.Text;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Computes lowercase hexadecimal digests of UTF-8 text for the hash utility API.
/// </summary>
public static class TextHasher
{
    /// <summary>
    /// Returns SHA-256, MD5, and SHA-1 hashes of <paramref name="text"/> encoded as UTF-8.
    /// </summary>
    public static HashTextResponse ComputeHashes(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return new HashTextResponse(
            Sha256: Convert.ToHexStringLower(SHA256.HashData(bytes)),
            Md5: Convert.ToHexStringLower(MD5.HashData(bytes)),
            Sha1: Convert.ToHexStringLower(SHA1.HashData(bytes)));
    }
}
