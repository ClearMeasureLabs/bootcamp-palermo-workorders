using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class FeatureFlagsCatalogTests
{
    [Test]
    public void BuildSnapshot_Should_ContainExactlyCatalogKeysWithCorrectValues_When_OptionsGiven()
    {
        var options = new DiagnosticsFeatureFlagsOptions { SampleFeatureA = false, SampleFeatureB = true };

        var snapshot = FeatureFlagsCatalog.BuildSnapshot(options);

        snapshot.Count.ShouldBe(FeatureFlagsCatalog.AllKeys.Count);
        snapshot.Keys.ShouldBe(FeatureFlagsCatalog.AllKeys);
        snapshot[FeatureFlagsCatalog.SampleFeatureAKey].ShouldBeFalse();
        snapshot[FeatureFlagsCatalog.SampleFeatureBKey].ShouldBeTrue();
    }

    [Test]
    public void BuildSnapshot_Should_EnumerateKeysInStableCatalogOrder_When_Iterating()
    {
        var snapshot = FeatureFlagsCatalog.BuildSnapshot(
            new DiagnosticsFeatureFlagsOptions { SampleFeatureA = true, SampleFeatureB = false });

        snapshot.Keys.SequenceEqual(FeatureFlagsCatalog.AllKeys).ShouldBeTrue();
    }
}
