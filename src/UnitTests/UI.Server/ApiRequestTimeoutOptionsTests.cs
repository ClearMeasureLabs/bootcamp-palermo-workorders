using ClearMeasure.Bootcamp.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ApiRequestTimeoutOptionsTests
{
    [Test]
    public void EffectiveTimeoutSeconds_Should_ReturnZero_When_TimeoutSecondsIsZero()
    {
        var sut = new ApiRequestTimeoutOptions { TimeoutSeconds = 0 };

        sut.EffectiveTimeoutSeconds.ShouldBe(0);
    }

    [Test]
    public void EffectiveTimeoutSeconds_Should_ReturnConfiguredValue_When_WithinRange()
    {
        var sut = new ApiRequestTimeoutOptions { TimeoutSeconds = 120 };

        sut.EffectiveTimeoutSeconds.ShouldBe(120);
    }

    [Test]
    public void EffectiveTimeoutSeconds_Should_ClampTo600_When_TimeoutSecondsExceedsMax()
    {
        var sut = new ApiRequestTimeoutOptions { TimeoutSeconds = 5000 };

        sut.EffectiveTimeoutSeconds.ShouldBe(600);
    }
}
