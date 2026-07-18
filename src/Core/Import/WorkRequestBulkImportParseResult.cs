namespace ClearMeasure.Bootcamp.Core.Import;

/// <summary>
/// Outcome of parsing a bulk-import CSV file.
/// </summary>
public sealed class WorkRequestBulkImportParseResult
{
    private WorkRequestBulkImportParseResult(bool success, string? error, IReadOnlyList<WorkRequestBulkImportRow> rows)
    {
        Success = success;
        Error = error;
        Rows = rows;
    }

    public bool Success { get; }

    public string? Error { get; }

    public IReadOnlyList<WorkRequestBulkImportRow> Rows { get; }

    public static WorkRequestBulkImportParseResult Ok(IReadOnlyList<WorkRequestBulkImportRow> rows) =>
        new(true, null, rows);

    public static WorkRequestBulkImportParseResult Fail(string error) =>
        new(false, error, Array.Empty<WorkRequestBulkImportRow>());
}
