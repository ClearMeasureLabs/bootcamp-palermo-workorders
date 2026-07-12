using System.Security.Cryptography;
using System.Text;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Computes UTF-8 text digests as lowercase hexadecimal strings.
/// </summary>
internal static class TextHashComputer
{
    /// <summary>
    /// Hashes <paramref name="text"/> using UTF-8 encoding and returns SHA-256, MD5, and SHA-1 digests.
    /// </summary>
    public static (string Sha256, string Md5, string Sha1) Compute(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes));
        var md5 = Convert.ToHexStringLower(MD5.HashData(bytes));
        var sha1 = Convert.ToHexStringLower(SHA1.HashData(bytes));
        return (sha256, md5, sha1);
    }
}
