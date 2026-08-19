namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON request body for <c>POST /api/tools/hash</c> and versioned routes.
/// </summary>
public sealed record HashTextRequest(string? Text);

/// <summary>
/// JSON response containing lowercase hex digests for SHA-256, MD5, and SHA-1 of the input text (UTF-8).
/// </summary>
public sealed record HashTextResponse(string Sha256, string Md5, string Sha1);
