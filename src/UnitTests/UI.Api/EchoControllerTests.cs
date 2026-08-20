using System.Net;
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
    public void Get_Should_ReturnJsonReflectingRequest_When_HttpContextPopulated()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/echo";
        context.Request.QueryString = new QueryString("?foo=bar&foo=baz");
        context.Request.Headers["X-Test"] = "a";
        context.Request.Headers["X-Multi"] = new[] { "1", "2" };
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");

        var controller = new EchoController
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");

        var payload = JsonSerializer.Deserialize<EchoResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Method.ShouldBe("GET");
        payload.Path.ShouldBe("/api/echo");
        payload.Query["foo"].ShouldBe(["bar", "baz"]);
        payload.Headers["X-Test"].ShouldBe(["a"]);
        payload.Headers["X-Multi"].ShouldBe(["1", "2"]);
        payload.ClientIp.ShouldBe("203.0.113.10");
    }

    [Test]
    public void Get_Should_IncludePathBaseInPath_When_PathBaseNonEmpty()
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
        var payload = JsonSerializer.Deserialize<EchoResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Path.ShouldBe("/app/api/echo");
    }

    [Test]
    public void Get_Should_ReturnNullClientIp_When_RemoteIpAddressMissing()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/echo";
        context.Connection.RemoteIpAddress = null;

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
        payload!.ClientIp.ShouldBeNull();
    }
}
