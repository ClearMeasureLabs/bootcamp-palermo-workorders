using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Request body for <c>POST /api/tools/hash</c>.
/// </summary>
public sealed class ToolsHashRequest
{
    /// <summary>
    /// Input text hashed as UTF-8 bytes. Required; an empty string yields well-defined digests.
    /// </summary>
    [Required(ErrorMessage = "The text field is required.")]
    public string? Text { get; set; }

    /// <summary>
    /// When <see langword="true"/>, weak MD5 and SHA-1 digests are included in the response.
    /// </summary>
    public bool IncludeLegacyHashes { get; set; }
}

/// <summary>
/// Digest values returned from <c>POST /api/tools/hash</c>.
/// </summary>
public sealed class ToolsHashResponse
{
    /// <summary>
    /// Lowercase hexadecimal SHA-256 digest of <see cref="ToolsHashRequest.Text"/> as UTF-8.
    /// </summary>
    public required string Sha256 { get; init; }

    /// <summary>
    /// Lowercase hexadecimal MD5 digest when legacy hashes were requested; otherwise omitted from JSON.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Md5 { get; init; }

    /// <summary>
    /// Lowercase hexadecimal SHA-1 digest when legacy hashes were requested; otherwise omitted from JSON.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Sha1 { get; init; }
}
