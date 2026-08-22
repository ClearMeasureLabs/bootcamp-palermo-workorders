using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class FeatureFlagsControllerTests
{
    [Test]
    public void Get_Should_ReturnJsonObjectOfAllFlags_When_Called()
    {
        var controller = new FeatureFlagsController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<Dictionary<string, bool>>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.ShouldBe(FeatureFlagsCatalog.GetAll().ToDictionary(static pair => pair.Key, static pair => pair.Value));
        payload["EnableAdvancedSearch"].ShouldBeTrue();
        payload["EnableLegacyReports"].ShouldBeFalse();
    }

    [Test]
    public void Get_Should_NotBindDiagnosticsFeatureFlagsOptions_When_Called()
    {
        var controller = new FeatureFlagsController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        using var doc = JsonDocument.Parse(content.Content!);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);
        doc.RootElement.TryGetProperty("sampleFeatureA", out _).ShouldBeFalse();
        doc.RootElement.TryGetProperty("sampleFeatureB", out _).ShouldBeFalse();
        doc.RootElement.TryGetProperty("EnableAdvancedSearch", out var advanced).ShouldBeTrue();
        advanced.GetBoolean().ShouldBeTrue();
        doc.RootElement.TryGetProperty("EnableLegacyReports", out var legacy).ShouldBeTrue();
        legacy.GetBoolean().ShouldBeFalse();
    }
}
