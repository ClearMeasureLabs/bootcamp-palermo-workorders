using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class FeaturesControllerTests
{
    [Test]
    public void Should_Return200AndFlatJsonDictionary_When_GetFlagsCalled()
    {
        var flags = new DiagnosticsFeatureFlagsOptions { SampleFeatureA = true, SampleFeatureB = false };
        var controller = CreateController(flags);

        var result = controller.GetFlags();

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(StatusCodes.Status200OK);
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
    public void Should_MatchResolverOutput_When_GetFlagsCalled()
    {
        var flags = new DiagnosticsFeatureFlagsOptions { SampleFeatureA = false, SampleFeatureB = true };
        var controller = CreateController(flags);

        var result = controller.GetFlags();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<Dictionary<string, bool>>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        var expected = FeatureFlagStatusResolver.Resolve(flags);
        payload!.Count.ShouldBe(expected.Count);
        foreach (var (key, value) in expected)
            payload[key].ShouldBe(value);
    }

    [Test]
    public void Should_IncludeEtagOrConditionalGetBehavior_When_GetFlagsCalled()
    {
        var flags = new DiagnosticsFeatureFlagsOptions { SampleFeatureA = true, SampleFeatureB = false };
        var httpContext = new DefaultHttpContext();
        var controller = CreateController(flags, httpContext);

        var first = controller.GetFlags();
        first.ShouldBeOfType<ContentResult>();
        httpContext.Response.Headers.ETag.ToString().ShouldNotBeNullOrWhiteSpace();

        var etag = httpContext.Response.Headers.ETag.ToString();
        var secondContext = new DefaultHttpContext();
        secondContext.Request.Headers.IfNoneMatch = etag;
        var secondController = CreateController(flags, secondContext);

        var second = secondController.GetFlags();

        second.ShouldBeOfType<StatusCodeResult>();
        ((StatusCodeResult)second).StatusCode.ShouldBe(StatusCodes.Status304NotModified);
    }

    private static FeaturesController CreateController(
        DiagnosticsFeatureFlagsOptions flags,
        HttpContext? httpContext = null)
    {
        return new FeaturesController(Options.Create(flags))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext ?? new DefaultHttpContext()
            }
        };
    }
}
