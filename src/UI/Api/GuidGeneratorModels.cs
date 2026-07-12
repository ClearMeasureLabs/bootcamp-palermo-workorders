namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON request body for <c>POST /api/tools/guid-generator</c>.
/// </summary>
public sealed record GuidGeneratorRequest(int? Count = null);

/// <summary>
/// JSON response for <c>POST /api/tools/guid-generator</c>.
/// </summary>
public sealed record GuidGeneratorResponse(int Count, IReadOnlyList<string> Guids);
