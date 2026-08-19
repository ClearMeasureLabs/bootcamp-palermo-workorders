using System.Net;
using System.Reflection;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UnitTests.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class FeaturesControllerTests
{
    [SetUp]
    public void SetUp()
    {
        ApplicationFeatureFlags.HydrateFrom(new DiagnosticsFeatureFlagsOptions
        {
            SampleFeatureA = true,
            SampleFeatureB = false
        });
    }

    [Test]
    public void Should_Return200AndJson_When_GetFlagsUnversioned()
    {
        var controller = CreateController();

        var result = controller.GetFlags();

        AssertJsonFlatMap(result);
    }

    [Test]
    public void Should_Return200AndJson_When_GetFlagsVersioned()
    {
        var controller = CreateController();

        var result = controller.GetFlags();

        AssertJsonFlatMap(result);
    }

    [Test]
    public void Should_ReturnAllKnownFlags_When_DictionaryPopulated()
    {
        ApplicationFeatureFlags.HydrateFrom(new DiagnosticsFeatureFlagsOptions
        {
            SampleFeatureA = true,
            SampleFeatureB = false
        });
        var controller = CreateController();

        var result = controller.GetFlags();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<Dictionary<string, bool>>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Count.ShouldBe(2);
        payload["SampleFeatureA"].ShouldBeTrue();
        payload["SampleFeatureB"].ShouldBeFalse();
    }

    [Test]
    public async Task Should_EnforceRateLimiting_When_MultipleRequests()
    {
        await using var factory = new RateLimitedApiWebApplicationFactory();
        using var client = factory.CreateClient();

        (await client.GetAsync("/api/features/flags")).StatusCode.ShouldBe(HttpStatusCode.OK);

        var limited = await client.GetAsync("/api/features/flags");
        limited.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Test]
    public void Should_AllowAnonymous_When_NoAuthRequired()
    {
        var method = typeof(FeaturesController).GetMethod(nameof(FeaturesController.GetFlags));
        method.ShouldNotBeNull();
        method!.GetCustomAttribute<AllowAnonymousAttribute>().ShouldNotBeNull();
    }

    [Test]
    public void Should_ApplyRateLimitingPolicy_When_ControllerRegistered()
    {
        var controllerAttribute = typeof(FeaturesController).GetCustomAttribute<EnableRateLimitingAttribute>();
        controllerAttribute.ShouldNotBeNull();
        controllerAttribute!.PolicyName.ShouldBe(ApiRateLimiting.PolicyName);
    }

    private static FeaturesController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    private static void AssertJsonFlatMap(IActionResult result)
    {
        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<Dictionary<string, bool>>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.ShouldContainKey("SampleFeatureA");
        payload.ShouldContainKey("SampleFeatureB");
    }
}
