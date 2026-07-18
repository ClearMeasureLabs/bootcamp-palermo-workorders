namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON response for <c>POST /api/v1.0/work-requests/bulk-import</c>.
/// </summary>
public sealed record WorkRequestBulkImportResponse(
    int CreatedCount,
    IReadOnlyList<WorkRequestBulkImportRowResult> Results);

/// <summary>
/// Per-row outcome for bulk import.
/// </summary>
public sealed record WorkRequestBulkImportRowResult(
    int LineNumber,
    bool Success,
    string? WorkRequestNumber,
    string? Error);
