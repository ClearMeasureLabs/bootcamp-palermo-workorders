namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// Resolves the church's local calendar timezone (America/Chicago) with a Windows fallback.
/// </summary>
public static class ChurchTimeZone
{
    private const string IanaId = "America/Chicago";
    private const string WindowsId = "Central Standard Time";

    /// <summary>
    /// Gets the America/Chicago timezone, falling back to Central Standard Time on Windows.
    /// </summary>
    public static TimeZoneInfo Chicago { get; } = ResolveChicago();

    /// <summary>
    /// Returns today's calendar date in America/Chicago for the given clock.
    /// </summary>
    public static DateOnly Today(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        var local = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), Chicago);
        return DateOnly.FromDateTime(local.DateTime);
    }

    /// <summary>
    /// Returns the coming Saturday in America/Chicago (today when today is Saturday).
    /// </summary>
    public static DateOnly ComingSaturday(TimeProvider timeProvider)
    {
        var today = Today(timeProvider);
        var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)today.DayOfWeek + 7) % 7;
        return today.AddDays(daysUntilSaturday);
    }

    private static TimeZoneInfo ResolveChicago()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(IanaId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(WindowsId);
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(WindowsId);
        }
    }
}
