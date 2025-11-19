using System;
using System.Globalization;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Extensions
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Use this to help with date time string comparisons in tests.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string ToTestDateTimeString(this DateTime? dateTime)
        {
            return dateTime?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
        }
    }
}
