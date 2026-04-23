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
    public void Get_Should_ReturnOk_WithExpectedShape()
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
        payload!.Count.ShouldBe(ApplicationFeatureFlags.All.Count);
        foreach (var kv in ApplicationFeatureFlags.All)
        {
            payload[kv.Key].ShouldBe(kv.Value);
        }
    }

    [Test]
    public void Get_Should_Return304_When_IfNoneMatchMatchesEtag()
    {
        var controller = new FeatureFlagsController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        _ = controller.Get();
        var etag = controller.HttpContext.Response.Headers.ETag.ToString();
        etag.ShouldNotBeNullOrEmpty();

        var secondController = new FeatureFlagsController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        secondController.HttpContext.Request.Headers.IfNoneMatch = etag;

        var second = secondController.Get();

        second.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(StatusCodes.Status304NotModified);
    }
}
