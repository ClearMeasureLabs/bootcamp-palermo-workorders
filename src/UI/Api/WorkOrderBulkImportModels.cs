namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON response for <c>POST /api/v1.0/work-orders/bulk-import</c>.
/// </summary>
public sealed record WorkOrderBulkImportResponse(
    int CreatedCount,
    IReadOnlyList<WorkOrderBulkImportRowResult> Results);

/// <summary>
/// Per-row outcome for bulk import.
/// </summary>
public sealed record WorkOrderBulkImportRowResult(
    int LineNumber,
    bool Success,
    string? WorkOrderNumber,
    string? Error);
