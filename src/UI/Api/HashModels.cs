namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON request body for <c>POST /api/tools/hash</c> and <c>POST /api/v1.0/tools/hash</c>.
/// </summary>
public sealed record HashTextRequest(string? Text);

/// <summary>
/// JSON response for <c>POST /api/tools/hash</c> and <c>POST /api/v1.0/tools/hash</c>.
/// </summary>
public sealed record HashTextResponse(string Sha256, string Md5, string Sha1);
