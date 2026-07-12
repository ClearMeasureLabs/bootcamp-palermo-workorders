namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON request body for <c>POST /api/tools/hash</c> and <c>POST /api/v1.0/tools/hash</c>.
/// </summary>
public sealed record HashTextRequest(string? Text);

/// <summary>
/// JSON response for hash computation. MD5 and SHA-1 are included for diagnostics only; not suitable for secrets.
/// </summary>
public sealed record HashTextResponse(string Sha256, string Md5, string Sha1);
