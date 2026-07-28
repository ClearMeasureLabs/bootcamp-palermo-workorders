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
    private static EchoController CreateController(HttpContext httpContext)
    {
        return new EchoController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private static EchoResponse Deserialize(ContentResult content) =>
        JsonSerializer.Deserialize<EchoResponse>(content.Content!, ConditionalGetEtag.JsonSerializerOptions)!;

    [Test]
    public void Get_ReturnsJsonWithRequestMethod_When_Called()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;

        var result = CreateController(context).Get();
        var payload = Deserialize(result.ShouldBeOfType<ContentResult>());

        payload.Method.ShouldBe("GET");
    }

    [Test]
    public void Get_ReturnsJsonWithRequestPath_When_Called()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/echo";

        var result = CreateController(context).Get();
        var payload = Deserialize(result.ShouldBeOfType<ContentResult>());

        payload.Path.ShouldBe("/api/echo");
    }

    [Test]
    public void Get_ReturnsJsonWithQueryString_When_Called()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?foo=bar&baz=1");

        var result = CreateController(context).Get();
        var payload = Deserialize(result.ShouldBeOfType<ContentResult>());

        payload.QueryString.ShouldBe("?foo=bar&baz=1");
    }

    [Test]
    public void Get_ReturnsJsonWithSchemeHostPort_When_Called()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost", 7174);
        context.Request.Protocol = HttpProtocol.Http2;

        var result = CreateController(context).Get();
        var payload = Deserialize(result.ShouldBeOfType<ContentResult>());

        payload.Scheme.ShouldBe("https");
        payload.Host.ShouldBe("localhost:7174");
        payload.Protocol.ShouldBe(HttpProtocol.Http2);
    }

    [Test]
    public void Get_ReturnsJsonWithRemoteIp_When_Called()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Loopback;

        var result = CreateController(context).Get();
        var payload = Deserialize(result.ShouldBeOfType<ContentResult>());

        payload.RemoteIp.ShouldBe("127.0.0.1");
    }

    [Test]
    public void Get_ReturnsJsonWithHeaders_When_Called()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.UserAgent = "EchoTestAgent/1.0";
        context.Request.Headers.Accept = "application/json";

        var result = CreateController(context).Get();
        var payload = Deserialize(result.ShouldBeOfType<ContentResult>());

        payload.Headers.ShouldContainKey("User-Agent");
        payload.Headers["User-Agent"].ShouldBe("EchoTestAgent/1.0");
        payload.Headers.ShouldContainKey("Accept");
        payload.Headers["Accept"].ShouldBe("application/json");
    }

    [Test]
    public void Get_ReturnsJsonWithParsedQuery_When_Called()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?foo=bar&tag=a&tag=b");

        var result = CreateController(context).Get();
        var payload = Deserialize(result.ShouldBeOfType<ContentResult>());

        payload.Query.ShouldContainKey("foo");
        payload.Query["foo"].ShouldBe(["bar"]);
        payload.Query.ShouldContainKey("tag");
        payload.Query["tag"].ShouldBe(["a", "b"]);
    }

    [Test]
    public void Get_ReturnsApplicationJsonContentType_When_Called()
    {
        var context = new DefaultHttpContext();

        var result = CreateController(context).Get();
        var content = result.ShouldBeOfType<ContentResult>();

        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldBe("application/json; charset=utf-8");
    }
}
