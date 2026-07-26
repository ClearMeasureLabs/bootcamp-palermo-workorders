using System.Globalization;
using System.Net;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UI.Server.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Api;

[TestFixture]
public class HelloControllerTests
{
    [Test]
    public void Should_ReturnOk_When_HelloEndpointCalled()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Test]
    public void Should_ReturnCorrectJsonPayload_When_HelloEndpointCalled()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.Content.ShouldBe("{\"message\":\"Hello, World!\"}");
    }

    [Test]
    public void Should_ReturnJsonContentType_When_HelloEndpointCalled()
    {
        var controller = new HelloController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
    }

    [Test]
    public async Task Should_AllowAnonymousAccess_When_HelloEndpointCalled()
    {
        await using var factory = new TunableApiRateLimitWebApplicationFactory(RateLimitSettings(5));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/hello");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_IncludeRateLimitHeaders_When_HelloEndpointCalled()
    {
        await using var factory = new TunableApiRateLimitWebApplicationFactory(RateLimitSettings(5));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/hello");

        response.Headers.TryGetValues(RateLimitingMiddleware.HeaderLimit, out var limit).ShouldBeTrue();
        response.Headers.TryGetValues(RateLimitingMiddleware.HeaderRemaining, out var remaining).ShouldBeTrue();
        response.Headers.TryGetValues(RateLimitingMiddleware.HeaderReset, out _).ShouldBeTrue();
        limit!.First().ShouldBe("5");
        int.Parse(remaining!.First(), NumberFormatInfo.InvariantInfo).ShouldBeGreaterThanOrEqualTo(0);
    }

    private static IReadOnlyDictionary<string, string?> RateLimitSettings(int permitLimit) =>
        new Dictionary<string, string?>
        {
            ["ApiRateLimiting:Enabled"] = "true",
            ["ApiRateLimiting:PermitLimit"] = permitLimit.ToString(NumberFormatInfo.InvariantInfo),
            ["ApiRateLimiting:WindowSeconds"] = "60",
            ["ApiRateLimiting:SegmentsPerWindow"] = "2",
            ["ApiRateLimiting:QueueLimit"] = "0"
        };
}
