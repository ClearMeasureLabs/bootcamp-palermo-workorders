using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class FeatureFlagsControllerTests
{
    [Test]
    public void Get_Should_ReturnJson_WithFlagMap_When_Called()
    {
        var flags = new DiagnosticsFeatureFlagsOptions { SampleFeatureA = true, SampleFeatureB = false };
        var controller = new FeatureFlagsController(Options.Create(flags))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        using var doc = JsonDocument.Parse(content.Content!);
        doc.RootElement.TryGetProperty("flags", out var flagsElem).ShouldBeTrue();
        flagsElem.GetProperty(FeatureFlagsCatalog.SampleFeatureAKey).GetBoolean().ShouldBeTrue();
        flagsElem.GetProperty(FeatureFlagsCatalog.SampleFeatureBKey).GetBoolean().ShouldBeFalse();
        var dto = JsonSerializer.Deserialize<FeatureFlagsResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        dto.ShouldNotBeNull();
        dto!.Flags[FeatureFlagsCatalog.SampleFeatureAKey].ShouldBeTrue();
        dto.Flags[FeatureFlagsCatalog.SampleFeatureBKey].ShouldBeFalse();
    }
}
