namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON request body for <c>POST /api/tools/hash</c> and <c>POST /api/v1.0/tools/hash</c>.
/// </summary>
public sealed record HashRequest(string? Text);

/// <summary>
/// JSON response for hash utility endpoints.
/// </summary>
public sealed record HashResponse(string Sha256, string Md5, string Sha1);
