using System.Globalization;
using System.Text;
using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Formats work orders as CSV with RFC-style escaping for commas, quotes, and newlines.
/// </summary>
public static class WorkOrderCsvWriter
{
    private const string Header = "Number,Title,Status,Creator,Assignee,Room,DueDate";

    /// <summary>
    /// Builds a CSV document for the given work orders.
    /// </summary>
    public static string Write(IEnumerable<WorkOrder> workOrders)
    {
        var builder = new StringBuilder();
        builder.AppendLine(Header);
        foreach (var workOrder in workOrders)
        {
            builder.Append(Escape(workOrder.Number));
            builder.Append(',');
            builder.Append(Escape(workOrder.Title));
            builder.Append(',');
            builder.Append(Escape(workOrder.Status.Key));
            builder.Append(',');
            builder.Append(Escape(workOrder.Creator?.UserName));
            builder.Append(',');
            builder.Append(Escape(workOrder.Assignee?.UserName));
            builder.Append(',');
            builder.Append(Escape(workOrder.RoomNumber));
            builder.Append(',');
            builder.Append(Escape(FormatDueDate(workOrder.DueDate)));
            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>
    /// Escapes a CSV field when it contains commas, quotes, or newlines.
    /// </summary>
    public static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (!NeedsQuoting(value))
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static bool NeedsQuoting(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');

    private static string FormatDueDate(DateOnly? dueDate)
    {
        return dueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
    }
}
