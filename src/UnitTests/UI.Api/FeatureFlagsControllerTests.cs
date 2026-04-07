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
    public void Get_Should_ReturnOk_WithFlagsMatchingRegistry()
    {
        var controller = new FeatureFlagsController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<FeatureFlagsResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Flags.ShouldNotBeEmpty();
        var byKey = payload.Flags.ToDictionary(static f => f.Key, static f => f.Enabled, StringComparer.Ordinal);
        foreach (var (key, enabled) in FeatureFlagsRegistry.GetSnapshot())
        {
            byKey[key].ShouldBe(enabled);
        }
    }
}
