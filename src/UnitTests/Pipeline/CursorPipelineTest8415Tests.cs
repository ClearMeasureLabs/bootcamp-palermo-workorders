using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Pipeline;

[TestFixture]
public class CursorPipelineTest8415Tests
{
    private const string Issue8415Marker = "8415";

    [Test]
    public void Should_MarkIssue8415_When_CursorPipelineProbeExecutes()
    {
        var marker = Issue8415Marker;

        var result = marker;

        result.ShouldBe("8415");
    }
}
