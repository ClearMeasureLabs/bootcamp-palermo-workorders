namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Optional request body for on-demand GUID generation.
/// </summary>
/// <param name="Count">Number of GUIDs to generate (1–100 inclusive; default 1).</param>
public sealed record GuidGeneratorRequest(int? Count = null);

/// <summary>
/// Response payload containing generated GUID strings in standard "D" format.
/// </summary>
/// <param name="Guids">Generated GUID values.</param>
public sealed record GuidGeneratorResponse(IReadOnlyList<string> Guids);
