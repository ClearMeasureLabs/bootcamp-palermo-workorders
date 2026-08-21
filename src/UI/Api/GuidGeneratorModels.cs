namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Optional request body for on-demand GUID generation.
/// </summary>
/// <param name="Count">Number of GUIDs to generate (default 1, max 100).</param>
public record GuidGeneratorRequest(int Count = 1);

/// <summary>
/// Response payload containing generated GUID strings.
/// </summary>
/// <param name="Count">Number of GUIDs returned.</param>
/// <param name="Guids">Generated GUID values as strings.</param>
public record GuidGeneratorResponse(int Count, string[] Guids);
