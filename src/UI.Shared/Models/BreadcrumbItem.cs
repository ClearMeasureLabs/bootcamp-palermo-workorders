namespace ClearMeasure.Bootcamp.UI.Shared.Models;

/// <summary>
/// Represents one segment in the breadcrumb trail.
/// </summary>
/// <param name="Label">Display text for the segment.</param>
/// <param name="Url">Navigation target for parent segments; null for the active page.</param>
/// <param name="IsActive">True when this segment is the current page.</param>
public sealed record BreadcrumbItem(string Label, string? Url, bool IsActive);
