using System.Text;
using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Export;

/// <summary>
/// Formats work orders as RFC 4180 CSV with columns Number,Title,Status,Assignee,DueDate.
/// </summary>
public static class WorkOrderCsvFormatter
{
    /// <summary>
    /// Header row for the export format.
    /// </summary>
    public const string Header = "Number,Title,Status,Assignee,DueDate";

    /// <summary>
    /// Formats <paramref name="workOrders"/> as CSV including the header row.
    /// </summary>
    public static string Format(IEnumerable<WorkOrder> workOrders)
    {
        ArgumentNullException.ThrowIfNull(workOrders);

        var sb = new StringBuilder();
        sb.AppendLine(Header);
        foreach (var workOrder in workOrders)
        {
            sb.Append(Escape(workOrder.Number));
            sb.Append(',');
            sb.Append(Escape(workOrder.Title));
            sb.Append(',');
            sb.Append(Escape(workOrder.Status.ToString()));
            sb.Append(',');
            sb.Append(Escape(workOrder.Assignee?.UserName));
            sb.Append(',');
            sb.Append(Escape(FormatDueDate(workOrder.DueDate)));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escapes a field per RFC 4180 (quote when needed; double embedded quotes).
    /// </summary>
    public static string Escape(string? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var needsQuotes = value.Contains(',')
                          || value.Contains('"')
                          || value.Contains('\r')
                          || value.Contains('\n');
        if (!needsQuotes)
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static string FormatDueDate(DateOnly? dueDate) =>
        dueDate?.ToString("yyyy-MM-dd") ?? string.Empty;
}
