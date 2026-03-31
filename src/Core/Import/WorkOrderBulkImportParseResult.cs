namespace ClearMeasure.Bootcamp.Core.Import;

/// <summary>
/// Outcome of parsing a bulk-import CSV file.
/// </summary>
public sealed class WorkOrderBulkImportParseResult
{
    private WorkOrderBulkImportParseResult(bool success, string? error, IReadOnlyList<WorkOrderBulkImportRow> rows)
    {
        Success = success;
        Error = error;
        Rows = rows;
    }

    public bool Success { get; }

    public string? Error { get; }

    public IReadOnlyList<WorkOrderBulkImportRow> Rows { get; }

    public static WorkOrderBulkImportParseResult Ok(IReadOnlyList<WorkOrderBulkImportRow> rows) =>
        new(true, null, rows);

    public static WorkOrderBulkImportParseResult Fail(string error) =>
        new(false, error, Array.Empty<WorkOrderBulkImportRow>());
}
