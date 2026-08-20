using System.Text;

namespace ClearMeasure.Bootcamp.Core.Import;

/// <summary>
/// Parses CSV text with an optional header row. Supports quoted fields and comma separators.
/// </summary>
public static class WorkOrderBulkImportCsvParser
{
    private static readonly string[] RequiredHeaders = ["Title", "Description", "CreatorUsername"];

    /// <summary>
    /// Parses the stream as UTF-8 (BOM allowed). Returns rows or an error message.
    /// </summary>
    public static WorkOrderBulkImportParseResult Parse(Stream stream, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true,
            bufferSize: 4096, leaveOpen: true);
        return Parse(reader, cancellationToken);
    }

    internal static WorkOrderBulkImportParseResult Parse(TextReader reader, CancellationToken cancellationToken = default)
    {
        var lineNumber = 0;
        string? headerLine = CsvLineReader.ReadLogicalLine(reader, ref lineNumber, cancellationToken);
        if (headerLine == null)
        {
            return WorkOrderBulkImportParseResult.Fail("CSV is empty.");
        }

        var columnIndex = CsvColumnIndex.FromHeader(headerLine);
        var missingColumn = columnIndex.FindMissingRequiredColumn(RequiredHeaders);
        if (missingColumn != null)
        {
            return WorkOrderBulkImportParseResult.Fail(
                $"Missing required column \"{missingColumn}\". Expected header: Title, Description, CreatorUsername, RoomNumber (optional).");
        }

        var rows = ParseDataRows(reader, columnIndex, ref lineNumber, cancellationToken);
        return WorkOrderBulkImportParseResult.Ok(rows);
    }

    private static List<WorkOrderBulkImportRow> ParseDataRows(
        TextReader reader,
        CsvColumnIndex columnIndex,
        ref int lineNumber,
        CancellationToken cancellationToken)
    {
        var rows = new List<WorkOrderBulkImportRow>();
        string? line;
        while ((line = CsvLineReader.ReadLogicalLine(reader, ref lineNumber, cancellationToken)) != null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddNonEmptyRow(rows, columnIndex, line, lineNumber);
        }

        return rows;
    }

    private static void AddNonEmptyRow(
        List<WorkOrderBulkImportRow> rows,
        CsvColumnIndex columnIndex,
        string line,
        int lineNumber)
    {
        if (!string.IsNullOrWhiteSpace(line))
        {
            rows.Add(columnIndex.ParseRow(line, lineNumber));
        }
    }

    private static string? NullIfWhitespace(string? s)
    {
        if (s == null)
        {
            return null;
        }

        var t = s.Trim();
        return t.Length == 0 ? null : t;
    }

    private sealed class CsvColumnIndex
    {
        private readonly Dictionary<string, int> _columns;

        private CsvColumnIndex(Dictionary<string, int> columns) => _columns = columns;

        public static CsvColumnIndex FromHeader(string headerLine)
        {
            var headerValues = CsvLineReader.SplitCsvLine(headerLine);
            var columnIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headerValues.Count; i++)
            {
                AddUniqueHeader(columnIndex, headerValues[i], i);
            }

            return new CsvColumnIndex(columnIndex);
        }

        private static void AddUniqueHeader(Dictionary<string, int> columnIndex, string rawName, int index)
        {
            var name = rawName.Trim();
            if (name.Length > 0 && !columnIndex.ContainsKey(name))
            {
                columnIndex[name] = index;
            }
        }

        public string? FindMissingRequiredColumn(IEnumerable<string> requiredHeaders)
        {
            foreach (var required in requiredHeaders)
            {
                if (!_columns.ContainsKey(required))
                {
                    return required;
                }
            }

            return null;
        }

        public WorkOrderBulkImportRow ParseRow(string line, int lineNumber)
        {
            var cells = CsvLineReader.SplitCsvLine(line);
            string? Cell(int index) => index >= 0 && index < cells.Count ? NullIfWhitespace(cells[index]) : null;

            var titleIx = _columns["Title"];
            var descIx = _columns["Description"];
            var creatorIx = _columns["CreatorUsername"];
            var roomIx = _columns.TryGetValue("RoomNumber", out var r) ? r : -1;

            return new WorkOrderBulkImportRow(
                lineNumber,
                Cell(titleIx),
                Cell(descIx),
                Cell(creatorIx),
                roomIx >= 0 ? Cell(roomIx) : null);
        }
    }

    private static class CsvLineReader
    {
        internal static string? ReadLogicalLine(TextReader reader, ref int lineNumber, CancellationToken cancellationToken)
        {
            var first = reader.ReadLine();
            if (first == null)
            {
                return null;
            }

            lineNumber++;
            var combined = new StringBuilder(first);
            AppendContinuationLines(reader, combined, ref lineNumber, cancellationToken);
            return combined.ToString();
        }

        private static void AppendContinuationLines(
            TextReader reader,
            StringBuilder combined,
            ref int lineNumber,
            CancellationToken cancellationToken)
        {
            while (HasUnclosedQuotes(combined.ToString()))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var next = reader.ReadLine();
                if (next == null)
                {
                    break;
                }

                lineNumber++;
                combined.Append('\n').Append(next);
            }
        }

        private static bool HasUnclosedQuotes(string line) => CountUnescapedQuotes(line) % 2 != 0;

        internal static int CountUnescapedQuotes(string s)
        {
            var count = 0;
            var i = 0;
            while (i < s.Length)
            {
                i += CountQuoteAt(s, i, ref count);
            }

            return count;
        }

        private static int CountQuoteAt(string s, int i, ref int count)
        {
            if (s[i] != '"')
            {
                return 1;
            }

            if (IsEscapedQuote(s, i))
            {
                return 2;
            }

            count++;
            return 1;
        }

        internal static List<string> SplitCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            var i = 0;
            while (i < line.Length)
            {
                i = AppendNextCsvToken(line, i, current, result);
            }

            result.Add(current.ToString());
            return result;
        }

        private static int AppendNextCsvToken(string line, int i, StringBuilder current, List<string> result)
        {
            if (line[i] == '"')
            {
                return AppendQuotedField(line, i + 1, current);
            }

            if (line[i] == ',')
            {
                result.Add(current.ToString());
                current.Clear();
                return i + 1;
            }

            current.Append(line[i]);
            return i + 1;
        }

        private static int AppendQuotedField(string line, int startIndex, StringBuilder current)
        {
            var i = startIndex;
            while (i < line.Length)
            {
                var next = AppendQuotedCharacter(line, i, current);
                if (next < 0)
                {
                    return i + 1;
                }

                i = next;
            }

            return i;
        }

        private static int AppendQuotedCharacter(string line, int i, StringBuilder current)
        {
            if (line[i] != '"')
            {
                current.Append(line[i]);
                return i + 1;
            }

            if (IsEscapedQuote(line, i))
            {
                current.Append('"');
                return i + 2;
            }

            return -1;
        }

        private static bool IsEscapedQuote(string s, int index) =>
            s[index] == '"' && index + 1 < s.Length && s[index + 1] == '"';
    }
}
