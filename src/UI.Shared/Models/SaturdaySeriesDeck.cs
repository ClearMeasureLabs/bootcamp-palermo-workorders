using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

/// <summary>
/// Detects a Saturday work-order series for B1 Deck chrome (count badge + prompt).
/// </summary>
public static class SaturdaySeriesDeck
{
    /// <summary>
    /// True when results share one title and every row has a Saturday due date.
    /// </summary>
    public static bool IsSaturdaySeries(IReadOnlyList<WorkOrderSearchResultRow> results)
    {
        if (results.Count < 2)
        {
            return false;
        }

        var title = results[0].Title;
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        foreach (var row in results)
        {
            if (!string.Equals(row.Title, title, StringComparison.Ordinal))
            {
                return false;
            }

            if (row.WorkOrder.DueDate is not { } due || due.DayOfWeek != DayOfWeek.Saturday)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Card label: Sat N for a Saturday series, otherwise the formatted due date (or empty).
    /// </summary>
    public static string CardDateLabel(WorkOrderSearchResultRow row, int index, bool isSaturdaySeries)
    {
        if (isSaturdaySeries)
        {
            return $"Sat {index + 1}";
        }

        return row.DueDateDisplay ?? string.Empty;
    }

    /// <summary>
    /// Prompt chrome for a Saturday series deck (display only).
    /// </summary>
    public static string SeriesPromptText(IReadOnlyList<WorkOrderSearchResultRow> results)
    {
        if (!IsSaturdaySeries(results))
        {
            return string.Empty;
        }

        var assignee = results[0].Assignee?.GetFullName() ?? results[0].Assignee?.UserName ?? "assignee";
        var title = results[0].Title ?? "work";
        var count = results.Count;
        return $"Schedule {assignee} for {title} — {count} Saturdays.";
    }
}
