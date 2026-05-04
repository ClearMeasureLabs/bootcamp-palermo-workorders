using System.Text.Json.Serialization;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Request body for <c>POST /api/tools/hash</c> and the versioned duplicate route.
/// </summary>
public sealed class ToolsHashRequest
{
    /// <summary>
    /// UTF-8 text to digest; required (empty string is allowed).
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

/// <summary>
/// JSON response carrying lowercase hexadecimal digests of the input UTF-8 bytes.
/// MD5 and SHA-1 are checksums only, not for security.
/// </summary>
public sealed class ToolsHashResponse
{
    /// <summary>
    /// SHA-256 digest (64 hex characters, lowercase).
    /// </summary>
    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }

    /// <summary>
    /// MD5 digest (32 hex characters, lowercase).
    /// </summary>
    [JsonPropertyName("md5")]
    public required string Md5 { get; init; }

    /// <summary>
    /// SHA-1 digest (40 hex characters, lowercase).
    /// </summary>
    [JsonPropertyName("sha1")]
    public required string Sha1 { get; init; }
}
