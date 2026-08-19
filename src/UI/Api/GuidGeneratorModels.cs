namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Optional JSON body for <c>POST /api/tools/guid-generator</c>.
/// </summary>
/// <param name="Count">Number of GUIDs to generate (default 1 when omitted).</param>
public sealed record GuidGeneratorRequest(int? Count = null);

/// <summary>
/// JSON payload returned by <c>POST /api/tools/guid-generator</c>.
/// </summary>
/// <param name="Guids">Generated GUID strings in standard <c>D</c> format.</param>
public sealed record GuidGeneratorResponse(string[] Guids);
