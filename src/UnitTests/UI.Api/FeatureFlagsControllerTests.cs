using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class FeatureFlagsControllerTests
{
    [Test]
    public void Get_Should_ReturnJson_WithFlagsFromOptions_When_Called()
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
        var payload = JsonSerializer.Deserialize<FeatureFlagsResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Flags.Count.ShouldBe(2);
        payload.Flags.ShouldContain(f => f.Name == "SampleFeatureA" && f.Enabled);
        payload.Flags.ShouldContain(f => f.Name == "SampleFeatureB" && !f.Enabled);
    }

    [Test]
    public void Get_Should_Return304_When_IfNoneMatchMatchesEtag()
    {
        var flags = new DiagnosticsFeatureFlagsOptions { SampleFeatureA = false, SampleFeatureB = true };
        var httpContext = new DefaultHttpContext();
        var controller = new FeatureFlagsController(Options.Create(flags))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var first = controller.Get();
        var firstContent = first.ShouldBeOfType<ContentResult>();
        var snapshot = JsonSerializer.Deserialize<FeatureFlagsResponse>(
            firstContent.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        snapshot.ShouldNotBeNull();
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(snapshot);

        httpContext.Request.Headers.IfNoneMatch = etag.ToString();
        var second = controller.Get();
        var notModified = second.ShouldBeOfType<StatusCodeResult>();
        notModified.StatusCode.ShouldBe(StatusCodes.Status304NotModified);
    }

    [Test]
    public void Get_Should_SetWeakEtagHeader_When_200()
    {
        var controller = new FeatureFlagsController(
                Options.Create(new DiagnosticsFeatureFlagsOptions { SampleFeatureA = true, SampleFeatureB = true }))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        _ = controller.Get();

        var etagValues = controller.Response.Headers.ETag;
        etagValues.Count.ShouldBe(1);
        EntityTagHeaderValue.TryParse(etagValues.ToString(), out var parsed).ShouldBeTrue();
        parsed!.IsWeak.ShouldBeTrue();
    }
}
