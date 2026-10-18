using ClearMeasure.Bootcamp.Core;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core;

[TestFixture]
public class TextTruncationTests
{
    [Test]
    public void WithEllipsis_Should_ReturnTextUnchanged_When_TextFits()
    {
        var result = TextTruncation.WithEllipsis("short", 10);

        result.ShouldBe("short");
    }

    [Test]
    public void WithEllipsis_Should_TruncateWithEllipsis_When_TextExceedsMaxLength()
    {
        var result = TextTruncation.WithEllipsis("abcdefghij", 7);

        result.ShouldBe("abcd...");
    }

    [Test]
    public void WithEllipsis_Should_ReturnEmptyString_When_TextIsNull()
    {
        var result = TextTruncation.WithEllipsis(null, 10);

        result.ShouldBe(string.Empty);
    }
}
