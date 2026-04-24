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
    public void Get_Should_ReturnJson_WithFeatureFlags_When_Called()
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
        var payload = JsonSerializer.Deserialize<RuntimeFeatureFlagsResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.FeatureFlags.SampleFeatureA.ShouldBeTrue();
        payload.FeatureFlags.SampleFeatureB.ShouldBeFalse();
    }

    [Test]
    public void Get_Should_Return304_When_IfNoneMatchMatchesEtag()
    {
        var flags = new DiagnosticsFeatureFlagsOptions { SampleFeatureA = false, SampleFeatureB = true };
        var controller = new FeatureFlagsController(Options.Create(flags))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var first = controller.Get();
        var firstContent = first.ShouldBeOfType<ContentResult>();
        var etag = controller.Response.Headers.ETag.ToString();
        etag.ShouldNotBeNullOrWhiteSpace();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.IfNoneMatch = etag;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.Response.Headers.Clear();

        var second = controller.Get();

        second.ShouldBeOfType<StatusCodeResult>();
        ((StatusCodeResult)second).StatusCode.ShouldBe(StatusCodes.Status304NotModified);
    }
}
