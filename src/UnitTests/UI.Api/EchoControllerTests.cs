using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UI.Shared;
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
        content.StatusCode.ShouldBe(200);
        var payload = Deserialize(content.Content!);
        payload.Method.ShouldBe("GET");
        payload.Path.ShouldBe("/api/echo");
        payload.Query["foo"].ShouldBe("bar");
        payload.Query["baz"].ShouldBe("1");
        payload.QueryString.ShouldBe("?foo=bar&baz=1");
    }

    [Test]
    public void Get_Should_IncludeSafeHeaders_When_Present()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/echo";
        context.Request.Headers.UserAgent = "TestAgent";
        context.Request.Headers.Accept = "application/json";
        context.Request.Headers["X-Test"] = "debug";
        var controller = new EchoController
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = Deserialize(content.Content!);
        payload.Headers["User-Agent"].ShouldBe("TestAgent");
        payload.Headers["Accept"].ShouldBe("application/json");
        payload.Headers["X-Test"].ShouldBe("debug");
    }

    [Test]
    public void Get_Should_OmitSensitiveHeaders_When_AuthorizationOrApiKeyPresent()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/echo";
        context.Request.Headers.Authorization = "Bearer secret";
        context.Request.Headers.Cookie = "session=abc";
        context.Request.Headers[ApiKeyConstants.HeaderName] = "secret-key";
        context.Request.Headers.Accept = "application/json";
        var controller = new EchoController
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = Deserialize(content.Content!);
        payload.Headers.ContainsKey("Authorization").ShouldBeFalse();
        payload.Headers.ContainsKey("Cookie").ShouldBeFalse();
        payload.Headers.ContainsKey(ApiKeyConstants.HeaderName).ShouldBeFalse();
        payload.Headers["Accept"].ShouldBe("application/json");
    }

    [Test]
    public void Get_Should_ReturnPathBaseAndPath_When_RequestHasPathBase()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.PathBase = "/app";
        context.Request.Path = "/api/echo";
        var controller = new EchoController
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = Deserialize(content.Content!);
        payload.PathBase.ShouldBe("/app");
        payload.Path.ShouldBe("/api/echo");
    }

    private static EchoResponse Deserialize(string json) =>
        JsonSerializer.Deserialize<EchoResponse>(json, ConditionalGetEtag.JsonSerializerOptions)
        ?? throw new InvalidOperationException("Failed to deserialize echo response.");
}
