using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Extensions
{
    public static class DateTimeTestExtensions
    {
        /// <summary>
        /// Converts a string representation of a date/time to a nullable DateTime, truncated to the minute.
        /// </summary>
        /// <param name="dateTimeString">The string to parse. Can be in various formats depending on culture.</param>
        /// <returns>
        /// A DateTime truncated to the minute (seconds set to 0), or null if the input is null/whitespace.
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown when the string cannot be parsed as a DateTime in any supported format.
        /// </exception>
        /// <remarks>
        /// This method attempts to parse date/time strings using multiple strategies in the following order:
        /// 
        /// 1. **Unicode Normalization**: First normalizes the input by replacing narrow no-break space (U+202F) 
        ///    and non-breaking space (U+00A0) with regular spaces, which can appear in some culture formats.
        /// 
        /// 2. **"G" Format with Current Culture**: Attempts to parse using the general date/time pattern (long date and short time)
        ///    for the current culture. Example: "11/20/2025 10:04:50 PM" (en-US) or "2025-11-20 10:04:50 p.m." (en-CA)
        /// 
        /// 3. **General Parse with Current Culture**: Uses DateTime.TryParse with the current culture, which is the most
        ///    flexible approach and handles most culture-specific formats automatically.
        /// 
        /// 4. **ISO-Style Format (yyyy-MM-dd h:mm:ss tt)**: Attempts to parse dates in ISO format with 12-hour time.
        ///    Example: "2025-11-20 3:57:55 PM"
        /// 
        /// 5. **Localized AM/PM Designators**: For cultures like en-CA that use "a.m."/"p.m." with periods,
        ///    converts these to uppercase "AM"/"PM" and retries parsing with InvariantCulture.
        /// 
        /// 6. **Regex Fallback for ISO Format**: Uses regex to extract date/time components from ISO-style dates
        ///    and manually constructs the DateTime, handling AM/PM conversion:
        ///    - PM hours 1-11: Add 12 to convert to 24-hour format
        ///    - AM hour 12: Convert to 0 (midnight)
        /// 
        /// 7. **Regex Fallback for US Format**: Uses regex to extract date/time components from US-style dates
        ///    (M/d/yyyy or MM/dd/yyyy format) and manually constructs the DateTime with AM/PM handling.
        /// 
        /// All successfully parsed dates are truncated to the minute (seconds and milliseconds set to 0) to
        /// facilitate easier comparison in tests where second-level precision is not required.
        /// 
        /// <example>
        /// Supported formats include:
        /// - "11/20/2025 10:04:50 PM" (en-US)
        /// - "2025-11-20 3:57:55 p.m." (en-CA with periods)
        /// - "2025-11-20 15:57:55" (24-hour format)
        /// - "20/11/2025 22:04:50" (en-GB)
        /// </example>
        /// </remarks>
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
            
            // Try parsing with the "G" format that matches the UI output for current culture
            if (DateTime.TryParseExact(normalized, "G", CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out var dt))
                return TruncateToMinute(dt);
            
            // Try general parsing with current culture (most flexible)
            if (DateTime.TryParse(normalized, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out dt))
                return TruncateToMinute(dt);
            
            // Try exact with ISO-style format (yyyy-MM-dd h:mm:ss tt)
            if (DateTime.TryParseExact(normalized, "yyyy-MM-dd h:mm:ss tt",
                CultureInfo.CurrentCulture, DateTimeStyles.None, out dt))
                return TruncateToMinute(dt);
            
            // Replace localized designators with invariant for cultures like en-CA
            var withInvariantDesignators = normalized
                .Replace("a.m.", "AM", StringComparison.OrdinalIgnoreCase)
                .Replace("p.m.", "PM", StringComparison.OrdinalIgnoreCase);
            
            if (DateTime.TryParseExact(withInvariantDesignators, "yyyy-MM-dd h:mm:ss tt",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return TruncateToMinute(dt);
            
            // Regex fallback for ISO-style format (yyyy-MM-dd)
            var isoMatch = Regex.Match(normalized, @"^(\d{4})-(\d{2})-(\d{2}) (\d{1,2}):(\d{2}):(\d{2})");
            if (isoMatch.Success)
            {
                int year = int.Parse(isoMatch.Groups[1].Value);
                int month = int.Parse(isoMatch.Groups[2].Value);
                int day = int.Parse(isoMatch.Groups[3].Value);
                int hour = int.Parse(isoMatch.Groups[4].Value);
                int min = int.Parse(isoMatch.Groups[5].Value);
                int sec = int.Parse(isoMatch.Groups[6].Value);
                
                // Adjust for PM/AM if present
                if (normalized.Contains("PM", StringComparison.OrdinalIgnoreCase) && hour < 12)
                    hour += 12;
                if (normalized.Contains("AM", StringComparison.OrdinalIgnoreCase) && hour == 12)
                    hour = 0;
                if (normalized.Contains("p.m.", StringComparison.OrdinalIgnoreCase) && hour < 12)
                    hour += 12;
                if (normalized.Contains("a.m.", StringComparison.OrdinalIgnoreCase) && hour == 12)
                    hour = 0;
                
                return TruncateToMinute(new DateTime(year, month, day, hour, min, sec));
            }
            
            // Regex fallback for en-US style format (M/d/yyyy or MM/dd/yyyy)
            var usMatch = Regex.Match(normalized, @"^(\d{1,2})/(\d{1,2})/(\d{4}) (\d{1,2}):(\d{2}):(\d{2})");
            if (usMatch.Success)
            {
                int month = int.Parse(usMatch.Groups[1].Value);
                int day = int.Parse(usMatch.Groups[2].Value);
                int year = int.Parse(usMatch.Groups[3].Value);
                int hour = int.Parse(usMatch.Groups[4].Value);
                int min = int.Parse(usMatch.Groups[5].Value);
                int sec = int.Parse(usMatch.Groups[6].Value);
                
                // Adjust for PM/AM if present
                if (normalized.Contains("PM", StringComparison.OrdinalIgnoreCase) && hour < 12)
                    hour += 12;
                if (normalized.Contains("AM", StringComparison.OrdinalIgnoreCase) && hour == 12)
                    hour = 0;
                
                return TruncateToMinute(new DateTime(year, month, day, hour, min, sec));
            }
            
            throw new FormatException($"The string '{dateTimeString}' (trimmed: '{normalized}') could not be parsed as a DateTime. Current culture: {CultureInfo.CurrentCulture.Name}");
        }

        /// <summary>
        /// Retrieves the text content from a page element identified by test ID and parses it as a DateTime.
        /// </summary>
        /// <param name="page">The Playwright page containing the element.</param>
        /// <param name="testId">The test ID of the element to retrieve.</param>
        /// <returns>A DateTime truncated to the minute, or null if the element's text is empty/whitespace.</returns>
        /// <remarks>
        /// This is a convenience method for acceptance tests that combines element retrieval and DateTime parsing.
        /// The text content is parsed using <see cref="ToTestDateTime"/> which supports multiple date formats.
        /// </remarks>
        public static async Task<DateTime?> GetDateTimeFromTestIdAsync(this IPage page, string testId)
        {
            var textContent = await page.GetByTestId(testId).TextContentAsync();
            return textContent.ToTestDateTime();
        }

        /// <summary>
        /// Truncates a DateTime to the minute by setting seconds and milliseconds to 0.
        /// </summary>
        /// <param name="dateTime">The DateTime to truncate.</param>
        /// <returns>A new DateTime with the same date and time up to the minute, with seconds set to 0.</returns>
        /// <remarks>
        /// This is useful for comparing DateTime values in tests where second-level precision is not required,
        /// reducing the likelihood of test failures due to minor timing differences.
        /// </remarks>
        public static DateTime TruncateToMinute(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0);
        }

        /// <summary>
        /// Truncates a nullable DateTime to the minute by setting seconds and milliseconds to 0.
        /// </summary>
        /// <param name="dateTime">The nullable DateTime to truncate.</param>
        /// <returns>
        /// A new DateTime with the same date and time up to the minute, with seconds set to 0, or null if the input is null.
        /// </returns>
        public static DateTime? TruncateToMinute(this DateTime? dateTime)
        {
            return dateTime?.TruncateToMinute();
        }
    }
}
