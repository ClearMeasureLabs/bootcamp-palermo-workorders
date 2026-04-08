namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>POST /api/tools/guid-generator</c> and the versioned route.
/// </summary>
/// <param name="Guids">New GUID strings in canonical "D" format (32 hex digits with hyphens).</param>
public record GuidGeneratorResponse(IReadOnlyList<string> Guids);
