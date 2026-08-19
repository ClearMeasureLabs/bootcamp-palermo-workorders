using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class EchoControllerTests
{
    [Test]
    public void Get_Should_ReturnJson_WithMethodPathAndQuery_When_RequestHasQueryString()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/echo";
        context.Request.QueryString = new QueryString("?foo=bar&baz=1");
        var controller = new EchoController
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = JsonSerializer.Deserialize<EchoResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Method.ShouldBe("GET");
        payload.Path.ShouldBe("/api/echo");
        payload.QueryString.ShouldBe("foo=bar&baz=1");
        payload.Query["foo"].ShouldBe("bar");
        payload.Query["baz"].ShouldBe("1");
    }

    [Test]
    public void Get_Should_IncludeSafeHeaders_When_Present()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["User-Agent"] = "TestAgent";
        context.Request.Headers["Accept"] = "application/json";
        context.Request.Headers["X-Test"] = "debug";
        var controller = new EchoController
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<EchoResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Headers["User-Agent"].ShouldBe("TestAgent");
        payload.Headers["Accept"].ShouldBe("application/json");
        payload.Headers["X-Test"].ShouldBe("debug");
    }

    [Test]
    public void Get_Should_OmitSensitiveHeaders_When_AuthorizationOrApiKeyPresent()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer secret";
        context.Request.Headers["Cookie"] = "session=abc";
        context.Request.Headers["X-Api-Key"] = "key123";
        context.Request.Headers["User-Agent"] = "TestAgent";
        var controller = new EchoController
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<EchoResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Headers.ContainsKey("Authorization").ShouldBeFalse();
        payload.Headers.ContainsKey("Cookie").ShouldBeFalse();
        payload.Headers.ContainsKey("X-Api-Key").ShouldBeFalse();
        payload.Headers["User-Agent"].ShouldBe("TestAgent");
    }

    [Test]
    public void Get_Should_ReturnPathBaseAndPath_When_RequestHasPathBase()
    {
        var context = new DefaultHttpContext();
        context.Request.PathBase = "/app";
        context.Request.Path = "/api/echo";
        var controller = new EchoController
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<EchoResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.PathBase.ShouldBe("/app");
        payload.Path.ShouldBe("/api/echo");
    }
}
