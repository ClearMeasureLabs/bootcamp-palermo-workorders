namespace ClearMeasure.Bootcamp.Core.Import;

/// <summary>
/// Outcome of parsing a bulk-import CSV file.
/// </summary>
public sealed class WorkOrderBulkImportParseResult
{
    private WorkOrderBulkImportParseResult(
        bool success,
        string? error,
        IReadOnlyList<WorkOrderBulkImportRow> rows,
        IReadOnlyList<WorkOrderBulkImportRowError> rowErrors)
    {
        Success = success;
        Error = error;
        Rows = rows;
        RowErrors = rowErrors;
    }

    public bool Success { get; }

    public string? Error { get; }

    public IReadOnlyList<WorkOrderBulkImportRow> Rows { get; }

    /// <summary>Per-row validation messages (file-level parse still succeeded).</summary>
    public IReadOnlyList<WorkOrderBulkImportRowError> RowErrors { get; }

    public static WorkOrderBulkImportParseResult Ok(
        IReadOnlyList<WorkOrderBulkImportRow> rows,
        IReadOnlyList<WorkOrderBulkImportRowError>? rowErrors = null) =>
        new(true, null, rows, rowErrors ?? []);

    public static WorkOrderBulkImportParseResult Fail(string error) =>
        new(false, error, [], []);
}
