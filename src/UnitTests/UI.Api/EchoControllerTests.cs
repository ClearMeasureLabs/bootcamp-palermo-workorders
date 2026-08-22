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
    public void Get_Should_ReturnJsonEchoResponse_When_Called()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = "/api/echo";
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost:7174");
        httpContext.Request.Protocol = "HTTP/1.1";
        var controller = new EchoController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
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
        payload!.Method.ShouldBe(HttpMethods.Get);
        payload.Path.ShouldBe("/api/echo");
        payload.Scheme.ShouldBe("https");
        payload.Host.ShouldBe("localhost:7174");
        payload.Protocol.ShouldBe("HTTP/1.1");
    }

    [Test]
    public void Get_Should_ReflectQueryString_When_QueryPresent()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = "/api/echo";
        httpContext.Request.QueryString = new QueryString("?foo=bar&n=1");
        var controller = new EchoController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<EchoResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.QueryString.ShouldBe("?foo=bar&n=1");
        payload.Query["foo"].ShouldBe("bar");
        payload.Query["n"].ShouldBe("1");
    }

    [Test]
    public void Get_Should_ReflectHeaders_When_HeadersPresent()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = "/api/echo";
        httpContext.Request.Headers["X-Trace-Id"] = "abc-123";
        httpContext.Request.Headers["Accept"] = "application/json";
        var controller = new EchoController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<EchoResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Headers.ShouldContainKey("X-Trace-Id");
        payload.Headers["X-Trace-Id"].ShouldBe("abc-123");
        payload.Headers.ShouldContainKey("Accept");
        payload.Headers["Accept"].ShouldBe("application/json");
    }
}
