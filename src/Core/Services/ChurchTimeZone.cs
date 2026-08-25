namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// Resolves the church's local calendar timezone (America/Chicago) with a Windows fallback.
/// </summary>
public static class ChurchTimeZone
{
    private const string IanaId = "America/Chicago";
    private const string WindowsId = "Central Standard Time";

    /// <summary>
    /// Gets America/Chicago, then Central Standard Time, with UTC as a safe final fallback.
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

    private static TimeZoneInfo ResolveChicago() =>
        FindTimeZone(IanaId)
        ?? FindTimeZone(WindowsId)
        ?? TimeZoneInfo.Utc;

    private static TimeZoneInfo? FindTimeZone(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }
    }
}
