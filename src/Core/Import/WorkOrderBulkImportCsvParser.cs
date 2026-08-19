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
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = CsvLineReader.ReadLogicalLine(reader, ref lineNumber, cancellationToken);
            if (line == null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            rows.Add(columnIndex.ParseRow(line, lineNumber));
        }

        return rows;
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
                var name = headerValues[i].Trim();
                if (name.Length > 0 && !columnIndex.ContainsKey(name))
                {
                    columnIndex[name] = i;
                }
            }

            return new CsvColumnIndex(columnIndex);
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

            return combined.ToString();
        }

        private static bool HasUnclosedQuotes(string line) => CountUnescapedQuotes(line) % 2 != 0;

        internal static int CountUnescapedQuotes(string s)
        {
            var count = 0;
            var i = 0;
            while (i < s.Length)
            {
                if (s[i] == '"')
                {
                    if (IsEscapedQuote(s, i))
                    {
                        i += 2;
                        continue;
                    }

                    count++;
                }

                i++;
            }

            return count;
        }

        internal static List<string> SplitCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            var i = 0;
            while (i < line.Length)
            {
                if (line[i] == '"')
                {
                    i = AppendQuotedField(line, i + 1, current);
                }
                else if (line[i] == ',')
                {
                    result.Add(current.ToString());
                    current.Clear();
                    i++;
                }
                else
                {
                    current.Append(line[i]);
                    i++;
                }
            }

            result.Add(current.ToString());
            return result;
        }

        private static int AppendQuotedField(string line, int startIndex, StringBuilder current)
        {
            var i = startIndex;
            while (i < line.Length)
            {
                if (line[i] == '"')
                {
                    if (IsEscapedQuote(line, i))
                    {
                        current.Append('"');
                        i += 2;
                        continue;
                    }

                    return i + 1;
                }

                current.Append(line[i]);
                i++;
            }

            return i;
        }

        private static bool IsEscapedQuote(string s, int index) =>
            s[index] == '"' && index + 1 < s.Length && s[index + 1] == '"';
    }
}
