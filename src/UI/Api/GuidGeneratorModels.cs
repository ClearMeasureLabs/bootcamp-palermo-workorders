namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Optional JSON body for <c>POST /api/tools/guid-generator</c>.
/// </summary>
public sealed record GuidGeneratorRequest(int? Count);

/// <summary>
/// JSON response containing generated GUID strings.
/// </summary>
public sealed record GuidGeneratorResponse(int Count, IReadOnlyList<string> Guids);
