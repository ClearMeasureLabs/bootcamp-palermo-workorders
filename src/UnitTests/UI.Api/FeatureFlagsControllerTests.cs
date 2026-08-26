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
    public void Get_Should_ReturnAllCatalogEntries_When_Called()
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
        foreach (var (name, enabled) in FeatureFlagsCatalog.All)
        {
            payload.ShouldContainKey(name);
            payload[name].ShouldBe(enabled);
        }

        payload.Count.ShouldBe(FeatureFlagsCatalog.All.Count);
    }

    [Test]
    public void All_Should_BeNonNull_When_CatalogAccessed()
    {
        FeatureFlagsCatalog.All.ShouldNotBeNull();
        FeatureFlagsCatalog.All.Count.ShouldBeGreaterThan(0);
    }

    [Test]
    public void Get_Should_SerializeFlagKeysAsCatalogNames_When_UsingWebJsonDefaults()
    {
        var controller = new FeatureFlagsController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        using var doc = JsonDocument.Parse(content.Content!);
        foreach (var key in FeatureFlagsCatalog.All.Keys)
        {
            doc.RootElement.TryGetProperty(key, out _).ShouldBeTrue($"expected key '{key}' in JSON");
        }
    }
}
