using System.Globalization;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Extensions;

[TestFixture]
public class DateTimeTestExtensionsTests
{
    [Test]
    public void ToTestDateTime_WhenNullOrWhitespace_ShouldReturnNull()
    {
        ((string?)null).ToTestDateTime().ShouldBeNull();
        "   ".ToTestDateTime().ShouldBeNull();
    }

    [Test]
    public void ToTestDateTime_WhenUsFormat_ShouldTruncateToMinute()
    {
        using var _ = new CultureScope("en-US");
        var result = "11/20/2025 10:04:50 PM".ToTestDateTime();
        result.ShouldBe(new DateTime(2025, 11, 20, 22, 4, 0));
    }

    [Test]
    public void ToTestDateTime_WhenIso12HourWithPeriods_ShouldParse()
    {
        using var _ = new CultureScope("en-CA");
        var result = "2025-11-20 3:57:55 p.m.".ToTestDateTime();
        result.ShouldBe(new DateTime(2025, 11, 20, 15, 57, 0));
    }

    [Test]
    public void ToTestDateTime_WhenUnparseable_ShouldThrowFormatException()
    {
        Should.Throw<FormatException>(() => "not-a-date".ToTestDateTime());
    }

    [Test]
    public void TruncateToMinute_ShouldZeroSeconds()
    {
        var truncated = new DateTime(2025, 1, 2, 3, 4, 5).TruncateToMinute();
        truncated.ShouldBe(new DateTime(2025, 1, 2, 3, 4, 0));
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _previous;

        public CultureScope(string cultureName)
        {
            _previous = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
        }

        public void Dispose() => CultureInfo.CurrentCulture = _previous;
    }
}
