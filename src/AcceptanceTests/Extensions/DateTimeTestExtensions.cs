using System;
using System.Globalization;

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
            
            var trimmed = dateTimeString.Trim();
            
            // Try parsing with the "G" format that matches the UI output
            if (DateTime.TryParseExact(trimmed, "G", CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out DateTime result))
            {
                return result;
            }
            
            // Fallback to general parsing
            if (DateTime.TryParse(trimmed, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out result))
            {
                return result;
            }
            
            throw new FormatException($"The string '{dateTimeString}' (trimmed: '{trimmed}') could not be parsed as a DateTime. Current culture: {CultureInfo.CurrentCulture.Name}");
        }

        public static async Task<DateTime?> GetDateTimeFromTestIdAsync(this IPage page, string testId)
        {
            var textContent = await page.GetByTestId(testId).TextContentAsync();
            return textContent.ToTestDateTime();
        }
    }
}
