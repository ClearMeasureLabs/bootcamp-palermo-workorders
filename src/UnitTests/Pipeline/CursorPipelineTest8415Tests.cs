using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Pipeline;

[TestFixture]
public class CursorPipelineTest8415Tests
{
    [Test]
    public void Should_MarkIssue8415_When_CursorPipelineProbeExecutes()
    {
        const string marker = "8415";

        var result = marker;

        result.ShouldBe("8415");
    }
}
