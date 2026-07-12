using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class FeatureFlagsSnapshotTests
{
    [Test]
    public void Should_MapAllCatalogKeys_When_OptionsProvided()
    {
        var options = new DiagnosticsFeatureFlagsOptions { SampleFeatureA = true, SampleFeatureB = false };

        var snapshot = FeatureFlagsSnapshot.FromOptions(options);

        foreach (var key in FeatureFlagsSnapshot.CatalogKeys)
        {
            snapshot.ContainsKey(key).ShouldBeTrue();
        }
        snapshot.Count.ShouldBe(FeatureFlagsSnapshot.CatalogKeys.Count);
    }

    [Test]
    public void Should_ReflectOptionValues_When_FlagsEnabledOrDisabled()
    {
        var options = new DiagnosticsFeatureFlagsOptions { SampleFeatureA = true, SampleFeatureB = false };

        var snapshot = FeatureFlagsSnapshot.FromOptions(options);

        snapshot["sampleFeatureA"].ShouldBeTrue();
        snapshot["sampleFeatureB"].ShouldBeFalse();
    }

    [Test]
    public void Should_ReflectInvertedOptionValues_When_FlagsToggled()
    {
        var options = new DiagnosticsFeatureFlagsOptions { SampleFeatureA = false, SampleFeatureB = true };

        var snapshot = FeatureFlagsSnapshot.FromOptions(options);

        snapshot["sampleFeatureA"].ShouldBeFalse();
        snapshot["sampleFeatureB"].ShouldBeTrue();
    }
}
