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
        string? headerLine = ReadLogicalLine(reader, ref lineNumber, cancellationToken);
        if (headerLine == null)
        {
            return WorkOrderBulkImportParseResult.Fail("CSV is empty.");
        }

        var headerValues = SplitCsvLine(headerLine);
        var columnIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headerValues.Count; i++)
        {
            var name = headerValues[i].Trim();
            if (name.Length > 0 && !columnIndex.ContainsKey(name))
            {
                columnIndex[name] = i;
            }
        }

        foreach (var required in RequiredHeaders)
        {
            if (!columnIndex.ContainsKey(required))
            {
                return WorkOrderBulkImportParseResult.Fail(
                    $"Missing required column \"{required}\". Expected header: Title, Description, CreatorUsername; optional columns: Instructions, RoomNumber.");
            }
        }

        var titleIx = columnIndex["Title"];
        var descIx = columnIndex["Description"];
        var creatorIx = columnIndex["CreatorUsername"];
        var instructionsIx = columnIndex.TryGetValue("Instructions", out var ins) ? ins : -1;
        var roomIx = columnIndex.TryGetValue("RoomNumber", out var r) ? r : -1;

        var rows = new List<WorkOrderBulkImportRow>();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = ReadLogicalLine(reader, ref lineNumber, cancellationToken);
            if (line == null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var cells = SplitCsvLine(line);
            string? Cell(int index) => index >= 0 && index < cells.Count ? NullIfWhitespace(cells[index]) : null;

            rows.Add(new WorkOrderBulkImportRow(
                lineNumber,
                Cell(titleIx),
                Cell(descIx),
                Cell(creatorIx),
                instructionsIx >= 0 ? Cell(instructionsIx) : null,
                roomIx >= 0 ? Cell(roomIx) : null));
        }

        return WorkOrderBulkImportParseResult.Ok(rows);
    }

    private static string? ReadLogicalLine(TextReader reader, ref int lineNumber, CancellationToken cancellationToken)
    {
        var first = reader.ReadLine();
        if (first == null)
        {
            return null;
        }

        lineNumber++;
        var combined = new StringBuilder(first);
        while (CountUnescapedQuotes(combined.ToString()) % 2 != 0)
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

    private static int CountUnescapedQuotes(string s)
    {
        var count = 0;
        var i = 0;
        while (i < s.Length)
        {
            if (s[i] == '"')
            {
                if (i + 1 < s.Length && s[i + 1] == '"')
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

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var i = 0;
        while (i < line.Length)
        {
            if (line[i] == '"')
            {
                i++;
                while (i < line.Length)
                {
                    if (line[i] == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"');
                            i += 2;
                            continue;
                        }

                        i++;
                        break;
                    }

                    current.Append(line[i]);
                    i++;
                }
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

    private static string? NullIfWhitespace(string? s)
    {
        if (s == null)
        {
            return null;
        }

        var t = s.Trim();
        return t.Length == 0 ? null : t;
    }
}
