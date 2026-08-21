using System.Globalization;
using System.Text.RegularExpressions;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Extensions
{
    public static class DateTimeTestExtensions
    {
        private static readonly Func<string, DateTime?>[] ParseStrategies =
        [
            TryParseGFormat,
            TryParseCurrentCulture,
            TryParseIso12Hour,
            TryParseIsoWithInvariantAmPm,
            TryParseIsoRegex,
            TryParseUsRegex
        ];

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
        public static DateTime? ToTestDateTime(this string? dateTimeString)
        {
            if (string.IsNullOrWhiteSpace(dateTimeString))
            {
                return null;
            }

            var normalized = NormalizeSpaces(dateTimeString);
            foreach (var strategy in ParseStrategies)
            {
                var parsed = strategy(normalized);
                if (parsed.HasValue)
                {
                    return TruncateToMinute(parsed.Value);
                }
            }

            throw new FormatException(
                $"The string '{dateTimeString}' (trimmed: '{normalized}') could not be parsed as a DateTime. Current culture: {CultureInfo.CurrentCulture.Name}");
        }

        /// <summary>
        /// Retrieves the text content from a page element identified by test ID and parses it as a DateTime.
        /// </summary>
        public static async Task<DateTime?> GetDateTimeFromTestIdAsync(this IPage page, string testId)
        {
            var textContent = await page.GetByTestId(testId).TextContentAsync();
            return textContent.ToTestDateTime();
        }

        /// <summary>
        /// Truncates a DateTime to the minute by setting seconds and milliseconds to 0.
        /// </summary>
        public static DateTime TruncateToMinute(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0);
        }

        /// <summary>
        /// Truncates a nullable DateTime to the minute by setting seconds and milliseconds to 0.
        /// </summary>
        public static DateTime? TruncateToMinute(this DateTime? dateTime)
        {
            return dateTime?.TruncateToMinute();
        }

        private static string NormalizeSpaces(string dateTimeString) =>
            dateTimeString
                .Replace('\u202F', ' ')
                .Replace('\u00A0', ' ')
                .Trim();

        private static DateTime? TryParseGFormat(string normalized) =>
            DateTime.TryParseExact(normalized, "G", CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out var dt)
                ? dt
                : null;

        private static DateTime? TryParseCurrentCulture(string normalized) =>
            DateTime.TryParse(normalized, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out var dt)
                ? dt
                : null;

        private static DateTime? TryParseIso12Hour(string normalized) =>
            DateTime.TryParseExact(normalized, "yyyy-MM-dd h:mm:ss tt", CultureInfo.CurrentCulture, DateTimeStyles.None, out var dt)
                ? dt
                : null;

        private static DateTime? TryParseIsoWithInvariantAmPm(string normalized)
        {
            var withInvariantDesignators = normalized
                .Replace("a.m.", "AM", StringComparison.OrdinalIgnoreCase)
                .Replace("p.m.", "PM", StringComparison.OrdinalIgnoreCase);

            return DateTime.TryParseExact(
                withInvariantDesignators,
                "yyyy-MM-dd h:mm:ss tt",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt)
                ? dt
                : null;
        }

        private static DateTime? TryParseIsoRegex(string normalized)
        {
            var match = Regex.Match(normalized, @"^(\d{4})-(\d{2})-(\d{2}) (\d{1,2}):(\d{2}):(\d{2})");
            if (!match.Success)
            {
                return null;
            }

            return BuildDateTimeFromMatch(match, yearGroup: 1, monthGroup: 2, dayGroup: 3, normalized);
        }

        private static DateTime? TryParseUsRegex(string normalized)
        {
            var match = Regex.Match(normalized, @"^(\d{1,2})/(\d{1,2})/(\d{4}) (\d{1,2}):(\d{2}):(\d{2})");
            if (!match.Success)
            {
                return null;
            }

            return BuildDateTimeFromMatch(match, yearGroup: 3, monthGroup: 1, dayGroup: 2, normalized);
        }

        private static DateTime BuildDateTimeFromMatch(Match match, int yearGroup, int monthGroup, int dayGroup, string normalized)
        {
            var year = int.Parse(match.Groups[yearGroup].Value);
            var month = int.Parse(match.Groups[monthGroup].Value);
            var day = int.Parse(match.Groups[dayGroup].Value);
            var hour = AdjustHourForMeridiem(int.Parse(match.Groups[4].Value), normalized);
            var min = int.Parse(match.Groups[5].Value);
            var sec = int.Parse(match.Groups[6].Value);
            return new DateTime(year, month, day, hour, min, sec);
        }

        private static int AdjustHourForMeridiem(int hour, string normalized)
        {
            if (IsPm(normalized) && hour < 12)
            {
                return hour + 12;
            }

            if (IsAm(normalized) && hour == 12)
            {
                return 0;
            }

            return hour;
        }

        private static bool IsPm(string normalized) =>
            normalized.Contains("PM", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("p.m.", StringComparison.OrdinalIgnoreCase);

        private static bool IsAm(string normalized) =>
            normalized.Contains("AM", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("a.m.", StringComparison.OrdinalIgnoreCase);
    }
}
