using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class FeatureFlagStatusResolverTests
{
    [Test]
    public void Should_ReturnAllCatalogKeys_When_ResolveCalled()
    {
        var options = new DiagnosticsFeatureFlagsOptions { SampleFeatureA = true, SampleFeatureB = false };

        var result = FeatureFlagStatusResolver.Resolve(options);

        result.Keys.OrderBy(k => k).ShouldBe(FeatureFlagCatalog.Entries.Keys.OrderBy(k => k).ToArray());
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    [TestCase(false, false)]
    public void Should_MapSampleFeatureAAndB_FromOptionsPermutations_When_ResolveCalled(
        bool sampleFeatureA,
        bool sampleFeatureB)
    {
        var options = new DiagnosticsFeatureFlagsOptions
        {
            SampleFeatureA = sampleFeatureA,
            SampleFeatureB = sampleFeatureB
        };

        var result = FeatureFlagStatusResolver.Resolve(options);

        result["sampleFeatureA"].ShouldBe(sampleFeatureA);
        result["sampleFeatureB"].ShouldBe(sampleFeatureB);
    }
}
