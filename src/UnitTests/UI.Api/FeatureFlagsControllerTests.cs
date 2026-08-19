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
    public void Get_Should_ReturnJson_WithFlagsFromStaticDictionary_When_Called()
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
        var expected = ApplicationFeatureFlags.GetAll();
        payload!.Count.ShouldBe(expected.Count);
        foreach (var (key, value) in expected)
            payload[key].ShouldBe(value);
    }

    [Test]
    public void Get_Should_SerializeInCamelCase_When_Returning()
    {
        var controller = new FeatureFlagsController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.Content.ShouldNotBeNull();
        content.Content!.ShouldContain("\"sampleFeatureA\"");
        content.Content.ShouldContain("\"sampleFeatureB\"");
    }

    [Test]
    public void Get_Should_ReturnEmptyObject_When_DictionaryIsEmpty()
    {
        var result = ConditionalGetEtag.JsonContent(new Dictionary<string, bool>());

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        content.Content.ShouldBe("{}");
    }
}
