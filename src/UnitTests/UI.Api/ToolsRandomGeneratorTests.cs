using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsRandomGeneratorTests
{
    [TestCase(null, "Int")]
    [TestCase("", "Int")]
    [TestCase("INT", "Int")]
    [TestCase("Long", "Long")]
    [TestCase("GUID", "Guid")]
    [TestCase("String", "String")]
    [TestCase("BYTES", "Bytes")]
    public void TryParseKind_Should_ParseOrDefault(string? raw, string expectedName)
    {
        var ok = ToolsRandomGenerator.TryParseKind(raw, out var kind);
        ok.ShouldBeTrue();
        kind.ToString().ShouldBe(expectedName);
    }

    [Test]
    public void TryParseKind_Should_ReturnFalse_ForUnknown()
    {
        var ok = ToolsRandomGenerator.TryParseKind("nope", out _);
        ok.ShouldBeFalse();
    }

    [Test]
    public void TryParseIntBounds_Should_Default_WhenOmitted()
    {
        var ok = ToolsRandomGenerator.TryParseIntBounds(null, null, out var min, out var max, out var err);
        ok.ShouldBeTrue();
        err.ShouldBeNull();
        min.ShouldBe(ToolsRandomGenerator.DefaultIntMinInclusive);
        max.ShouldBe(ToolsRandomGenerator.DefaultIntMaxExclusive);
    }

    [Test]
    public void TryParseIntBounds_Should_ReturnError_WhenMinNotLessThanMax()
    {
        var ok = ToolsRandomGenerator.TryParseIntBounds("5", "5", out _, out _, out var err);
        ok.ShouldBeFalse();
        err.ShouldNotBeNull();
        err!.ShouldContain("min");
    }

    [Test]
    public void TryParseLongBounds_Should_ReturnError_WhenMinNotLessThanMax()
    {
        var ok = ToolsRandomGenerator.TryParseLongBounds("10", "9", out _, out _, out var err);
        ok.ShouldBeFalse();
        err.ShouldNotBeNull();
    }

    [Test]
    public void TryParseBoundedInt_Should_UseDefaultLength_WhenLengthOmitted()
    {
        var ok = ToolsRandomGenerator.TryParseBoundedInt(
            null,
            0,
            ToolsRandomGenerator.MaxStringOrByteLength,
            "length",
            out var value,
            out var err);
        ok.ShouldBeTrue();
        err.ShouldBeNull();
        value.ShouldBe(ToolsRandomGenerator.DefaultStringOrByteLength);
    }

    [Test]
    public void TryValidateCharset_Should_ReturnError_WhenTooLong()
    {
        var huge = new string('a', ToolsRandomGenerator.MaxCharsetLength + 1);
        var ok = ToolsRandomGenerator.TryValidateCharset(huge, out _, out var err);
        ok.ShouldBeFalse();
        err.ShouldNotBeNull();
    }

    [Test]
    public void NextString_Should_ReturnEmpty_ForZeroLength()
    {
        var r = new Random(42);
        var s = ToolsRandomGenerator.NextString(r, 0, "abc");
        s.ShouldBe(string.Empty);
    }

}
