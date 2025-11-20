using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Extensions
{
    public static class DateTimeTestExtensions
    {
        public static DateTime? ToTestDateTime(this string? dateTimeString)
        {
            if (string.IsNullOrWhiteSpace(dateTimeString))
            {
                return null;
            }
            
            var normalized = dateTimeString
                .Replace('\u202F', ' ')
                .Replace('\u00A0', ' ')
                .Trim();
            
            // Try exact with current culture pattern
            if (DateTime.TryParseExact(normalized, "yyyy-MM-dd h:mm:ss tt",
                CultureInfo.CurrentCulture, DateTimeStyles.None, out var dt))
                return TruncateToMinute(dt);
            
            // Replace localized designators with invariant
            var withInvariantDesignators = normalized
                .Replace("a.m.", "AM", StringComparison.OrdinalIgnoreCase)
                .Replace("p.m.", "PM", StringComparison.OrdinalIgnoreCase);
            
            if (DateTime.TryParseExact(withInvariantDesignators, "yyyy-MM-dd h:mm:ss tt",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return TruncateToMinute(dt);
            
            // Regex fallback for robust parsing
            var match = Regex.Match(normalized, @"^(\d{4})-(\d{2})-(\d{2}) (\d{1,2}):(\d{2}):(\d{2})");
            if (match.Success)
            {
                int year = int.Parse(match.Groups[1].Value);
                int month = int.Parse(match.Groups[2].Value);
                int day = int.Parse(match.Groups[3].Value);
                int hour = int.Parse(match.Groups[4].Value);
                int min = int.Parse(match.Groups[5].Value);
                int sec = int.Parse(match.Groups[6].Value);
                
                // Adjust for PM if present
                if (normalized.Contains("p.m.", StringComparison.OrdinalIgnoreCase) && hour < 12)
                    hour += 12;
                if (normalized.Contains("a.m.", StringComparison.OrdinalIgnoreCase) && hour == 12)
                    hour = 0;
                
                return TruncateToMinute(new DateTime(year, month, day, hour, min, sec));
            }
            
            throw new FormatException($"The string '{dateTimeString}' (trimmed: '{normalized}') could not be parsed as a DateTime. Current culture: {CultureInfo.CurrentCulture.Name}");
        }

        public static async Task<DateTime?> GetDateTimeFromTestIdAsync(this IPage page, string testId)
        {
            var textContent = await page.GetByTestId(testId).TextContentAsync();
            return textContent.ToTestDateTime();
        }

        public static DateTime TruncateToMinute(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0);
        }

        public static DateTime? TruncateToMinute(this DateTime? dateTime)
        {
            return dateTime?.TruncateToMinute();
        }
    }
}
