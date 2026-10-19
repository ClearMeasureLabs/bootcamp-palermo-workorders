using ClearMeasure.Bootcamp.UI.Shared.Formatting;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Formatting;

[TestFixture]
public class FileSizeFormatterTests
{
	[TestCase(0L, "0 B")]
	[TestCase(1L, "1 B")]
	[TestCase(512L, "512 B")]
	[TestCase(1023L, "1023 B")]
	public void Format_WhenBelowOneKb_ReturnsBytes(long bytes, string expected)
	{
		FileSizeFormatter.Format(bytes).ShouldBe(expected);
	}

	[TestCase(1024L, "1.0 KB")]
	[TestCase(1536L, "1.5 KB")]
	[TestCase(1024L * 1024L - 1, "1024.0 KB")]
	public void Format_WhenAtOrAboveOneKbBelowOneMb_ReturnsKilobytes(long bytes, string expected)
	{
		FileSizeFormatter.Format(bytes).ShouldBe(expected);
	}

	[TestCase(1024L * 1024L, "1.0 MB")]
	[TestCase(2L * 1024L * 1024L, "2.0 MB")]
	[TestCase(1024L * 1024L * 1024L - 1, "1024.0 MB")]
	public void Format_WhenAtOrAboveOneMbBelowOneGb_ReturnsMegabytes(long bytes, string expected)
	{
		FileSizeFormatter.Format(bytes).ShouldBe(expected);
	}

	[TestCase(1024L * 1024L * 1024L, "1.0 GB")]
	[TestCase(5L * 1024L * 1024L * 1024L / 2, "2.5 GB")]
	public void Format_WhenAtOrAboveOneGb_ReturnsGigabytes(long bytes, string expected)
	{
		FileSizeFormatter.Format(bytes).ShouldBe(expected);
	}
}
