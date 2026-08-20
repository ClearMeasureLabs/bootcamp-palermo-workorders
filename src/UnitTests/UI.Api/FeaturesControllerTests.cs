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
    [Test]
    public void Get_Should_Return200Json_WithFlatFlagDictionary_When_Called()
    {
        var controller = new FeaturesController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(StatusCodes.Status200OK);
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");

        using var doc = JsonDocument.Parse(content.Content!);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);
        foreach (var property in doc.RootElement.EnumerateObject())
        {
            property.Name[0].ShouldBe(char.ToLowerInvariant(property.Name[0]));
            (property.Value.ValueKind == JsonValueKind.True
                || property.Value.ValueKind == JsonValueKind.False).ShouldBeTrue();
        }
    }

    [Test]
    public void Get_Should_ReturnAllStaticFlags_WithExpectedDefaults_When_Called()
    {
        var controller = new FeaturesController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<Dictionary<string, bool>>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Count.ShouldBe(ApplicationFeatureFlags.All.Count);
        foreach (var (key, expected) in ApplicationFeatureFlags.All)
        {
            payload.TryGetValue(key, out var actual).ShouldBeTrue($"missing flag '{key}'");
            actual.ShouldBe(expected);
        }
    }
}
