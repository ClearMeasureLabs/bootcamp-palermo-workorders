using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class FeaturesControllerTests
{
    [SetUp]
    public void SetUp()
    {
        FeatureFlagRegistry.HydrateFrom(new DiagnosticsFeatureFlagsOptions
        {
            SampleFeatureA = false,
            SampleFeatureB = false
        });
    }

    [Test]
    public void Get_Should_Return200AndJson_When_Called()
    {
        FeatureFlagRegistry.HydrateFrom(new DiagnosticsFeatureFlagsOptions
        {
            SampleFeatureA = true,
            SampleFeatureB = false
        });
        var controller = new FeaturesController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(StatusCodes.Status200OK);
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        content.Content.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void Get_Should_ExposeAllFlagsWithCurrentState_When_Called()
    {
        FeatureFlagRegistry.HydrateFrom(new DiagnosticsFeatureFlagsOptions
        {
            SampleFeatureA = true,
            SampleFeatureB = false
        });
        var controller = new FeaturesController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        using var doc = JsonDocument.Parse(content.Content!);
        doc.RootElement.GetProperty("sampleFeatureA").GetBoolean().ShouldBeTrue();
        doc.RootElement.GetProperty("sampleFeatureB").GetBoolean().ShouldBeFalse();
    }

    [Test]
    public void Get_Should_UseConditionalGetEtagSerialization_When_Called()
    {
        FeatureFlagRegistry.HydrateFrom(new DiagnosticsFeatureFlagsOptions
        {
            SampleFeatureA = true,
            SampleFeatureB = false
        });
        var controller = new FeaturesController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var expected = JsonSerializer.Serialize(
            FeatureFlagRegistry.GetSnapshot(),
            ConditionalGetEtag.JsonSerializerOptions);
        content.Content.ShouldBe(expected);
    }
}
