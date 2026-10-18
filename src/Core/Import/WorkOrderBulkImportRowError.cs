namespace ClearMeasure.Bootcamp.Core.Import;

/// <summary>
/// Per-row validation failure produced while parsing a bulk-import CSV.
/// </summary>
public sealed record WorkOrderBulkImportRowError(int LineNumber, string Message);
