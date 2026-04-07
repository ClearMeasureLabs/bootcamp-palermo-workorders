using System.Text.Json.Serialization;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON body for <c>POST /api/tools/hash</c>.
/// </summary>
public sealed record ToolsHashRequest(
    string? Text,
    bool IncludeLegacyHashes = false);

/// <summary>
/// JSON response for <c>POST /api/tools/hash</c>.
/// </summary>
public sealed record ToolsHashResponse
{
    /// <summary>
    /// Lowercase hexadecimal SHA-256 digest of <see cref="ToolsHashRequest.Text"/> using UTF-8 encoding.
    /// </summary>
    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }

    /// <summary>
    /// Present when <see cref="ToolsHashRequest.IncludeLegacyHashes"/> was true.
    /// </summary>
    [JsonPropertyName("md5")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Md5 { get; init; }

    /// <summary>
    /// Present when <see cref="ToolsHashRequest.IncludeLegacyHashes"/> was true.
    /// </summary>
    [JsonPropertyName("sha1")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Sha1 { get; init; }
}
