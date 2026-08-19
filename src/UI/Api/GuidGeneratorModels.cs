namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Optional JSON body for <c>POST /api/tools/guid-generator</c>.
/// </summary>
/// <param name="Count">Number of GUIDs to generate (1–100). Omitted or null defaults to 1.</param>
public sealed record GuidGeneratorRequest(int? Count = null);

/// <summary>
/// JSON response containing generated GUID strings in standard <c>D</c> format.
/// </summary>
/// <param name="Guids">Generated GUID values.</param>
public sealed record GuidGeneratorResponse(string[] Guids);
