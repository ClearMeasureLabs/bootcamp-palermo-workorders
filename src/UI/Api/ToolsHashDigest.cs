using System.Security.Cryptography;
using System.Text;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Computes UTF-8 string digests for the tools hash API.
/// </summary>
internal static class ToolsHashDigest
{
    internal static ToolsHashResponse Compute(string text, bool includeLegacyHashes)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var sha256Hex = ToHexLower(SHA256.HashData(bytes));

        if (!includeLegacyHashes)
        {
            return new ToolsHashResponse { Sha256 = sha256Hex };
        }

        return new ToolsHashResponse
        {
            Sha256 = sha256Hex,
            Md5 = ToHexLower(MD5.HashData(bytes)),
            Sha1 = ToHexLower(SHA1.HashData(bytes))
        };
    }

    private static string ToHexLower(byte[] hash) => Convert.ToHexString(hash).ToLowerInvariant();
}
