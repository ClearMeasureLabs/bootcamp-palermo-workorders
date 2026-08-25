using System.Globalization;
using System.Text;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.McpServer.Tools;

/// <summary>
/// Resolves and formats due dates for dated work-order scheduling tools.
/// </summary>
internal static class DatedWorkOrderScheduling
{
    /// <summary>
    /// Builds due dates from an explicit comma-separated list or consecutive Chicago Saturdays.
    /// </summary>
    internal static (IReadOnlyList<DateOnly> Dates, string? Error) ResolveDueDates(
        TimeProvider timeProvider,
        string? dueDates,
        int saturdayCount)
    {
        if (!string.IsNullOrWhiteSpace(dueDates))
        {
            return TryParseDueDateList(dueDates);
        }

        if (saturdayCount <= 0)
        {
            return ([], "saturdayCount must be a positive number.");
        }

        return (BuildComingSaturdays(timeProvider, saturdayCount), null);
    }

    /// <summary>
    /// Formats a successful or failed dated create result for the AI reply.
    /// </summary>
    internal static string FormatResult(CreateDatedWorkOrdersResult result)
    {
        if (!result.Success)
        {
            return result.Message;
        }

        var reply = new StringBuilder();
        reply.AppendLine($"Created {result.WorkOrders.Count} work orders:");
        foreach (var item in result.WorkOrders)
        {
            reply.AppendLine($"{item.Number} due {item.DueDate:yyyy-MM-dd}");
        }

        return reply.ToString().TrimEnd();
    }

    /// <summary>
    /// Coming Saturday in America/Chicago, then increments of seven days.
    /// </summary>
    internal static IReadOnlyList<DateOnly> BuildComingSaturdays(TimeProvider timeProvider, int count)
    {
        var first = ChurchTimeZone.ComingSaturday(timeProvider);
        var dates = new List<DateOnly>(count);
        for (var i = 0; i < count; i++)
        {
            dates.Add(first.AddDays(7 * i));
        }

        return dates;
    }

    private static (IReadOnlyList<DateOnly> Dates, string? Error) TryParseDueDateList(string dueDates)
    {
        var list = new List<DateOnly>();
        foreach (var part in dueDates.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!DateOnly.TryParseExact(part, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var value))
            {
                return ([], $"Invalid due date '{part}'. Use yyyy-MM-dd.");
            }

            list.Add(value);
        }

        return (list, null);
    }

    internal static bool TryParseOptionalDueDate(string? dueDate, out DateOnly? parsed, out string? error)
    {
        parsed = null;
        error = null;
        if (string.IsNullOrWhiteSpace(dueDate))
        {
            return true;
        }

        if (DateOnly.TryParseExact(dueDate.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var value))
        {
            parsed = value;
            return true;
        }

        error = $"Invalid due date '{dueDate}'. Use yyyy-MM-dd.";
        return false;
    }
}
