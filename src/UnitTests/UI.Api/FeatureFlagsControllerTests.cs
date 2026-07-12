using System.Net;
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
    public void Get_Should_ReturnJson_WithFlatFeatureFlagMap_When_Called()
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
        var payload = JsonSerializer.Deserialize<Dictionary<string, bool>>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!["sampleFeatureA"].ShouldBeTrue();
        payload["sampleFeatureB"].ShouldBeFalse();
    }

    [Test]
    public void Get_Should_Return304NotModified_When_IfNoneMatchMatchesEtag()
    {
        var flags = new DiagnosticsFeatureFlagsOptions { SampleFeatureA = true, SampleFeatureB = false };
        var httpContext = new DefaultHttpContext();
        var controller = new FeatureFlagsController(Options.Create(flags))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var first = controller.Get();
        first.ShouldBeOfType<ContentResult>();
        var etag = httpContext.Response.Headers.ETag.ToString();
        etag.ShouldNotBeNullOrEmpty();

        httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.IfNoneMatch = etag;
        controller = new FeatureFlagsController(Options.Create(flags))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var second = controller.Get();

        second.ShouldBeOfType<StatusCodeResult>();
        ((StatusCodeResult)second).StatusCode.ShouldBe(StatusCodes.Status304NotModified);
    }
}
