namespace ClearMeasure.Bootcamp.Core.Import;

/// <summary>
/// One logical row from a work-request bulk-import CSV after parsing (line numbers are 1-based file lines).
/// </summary>
public sealed record WorkRequestBulkImportRow(int LineNumber, string? Title, string? Description, string? CreatorUsername, string? RoomNumber);
