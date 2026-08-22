using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class FeatureFlagsCatalogTests
{
    [Test]
    public void GetAll_Should_ReturnAllSeededFlags_When_CatalogInitialized()
    {
        var flags = FeatureFlagsCatalog.GetAll();

        flags.Count.ShouldBeGreaterThanOrEqualTo(2);
        flags.ShouldContainKey("EnableAdvancedSearch");
        flags.ShouldContainKey("EnableLegacyReports");
        flags["EnableAdvancedSearch"].ShouldBeTrue();
        flags["EnableLegacyReports"].ShouldBeFalse();
    }
}
