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
    private static EchoController CreateController(HttpContext httpContext) =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

    [Test]
    public void Should_ReturnEchoResponse_WithRequestMethod()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;

        var result = CreateController(context).Get();

        var payload = DeserializePayload(result);
        payload.Method.ShouldBe("POST");
    }

    [Test]
    public void Should_ReturnEchoResponse_WithPath()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/echo";

        var result = CreateController(context).Get();

        var payload = DeserializePayload(result);
        payload.Path.ShouldBe("/api/echo");
    }

    [Test]
    public void Should_ReturnEchoResponse_WithQueryString()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?foo=bar&baz=qux");

        var result = CreateController(context).Get();

        var payload = DeserializePayload(result);
        payload.QueryString.ShouldBe("?foo=bar&baz=qux");
        payload.Query["foo"].ShouldBe(["bar"]);
        payload.Query["baz"].ShouldBe(["qux"]);
    }

    [Test]
    public void Should_ReturnEchoResponse_WithClientIp()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");

        var result = CreateController(context).Get();

        var payload = DeserializePayload(result);
        payload.ClientIp.ShouldBe("203.0.113.10");
    }

    [Test]
    public void Should_ReturnEchoResponse_WithClientIpFromXForwardedFor()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.5, 10.0.0.1";

        var result = CreateController(context).Get();

        var payload = DeserializePayload(result);
        payload.ClientIp.ShouldBe("198.51.100.5");
    }

    [Test]
    public void Should_SelectAndIncludeWhitelistedHeaders()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["Accept"] = "application/json";
        context.Request.Headers["User-Agent"] = "EchoTest/1.0";
        context.Request.Headers["Host"] = "localhost";
        context.Request.Headers["X-Correlation-ID"] = "corr-123";
        context.Request.Headers["X-Custom-Header"] = "ignored";

        var result = CreateController(context).Get();

        var payload = DeserializePayload(result);
        payload.Headers["Accept"].ShouldBe("application/json");
        payload.Headers["User-Agent"].ShouldBe("EchoTest/1.0");
        payload.Headers["Host"].ShouldBe("localhost");
        payload.Headers["X-Correlation-ID"].ShouldBe("corr-123");
        payload.Headers.ContainsKey("X-Custom-Header").ShouldBeFalse();
    }

    [Test]
    public void Should_MaskSensitiveHeaderValues()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer secret-token";
        context.Request.Headers["X-API-Key"] = "super-secret";
        context.Request.Headers["Cookie"] = "session=abc123";

        var result = CreateController(context).Get();

        var payload = DeserializePayload(result);
        payload.Headers["Authorization"].ShouldBe("[REDACTED]");
        payload.Headers["X-API-Key"].ShouldBe("[REDACTED]");
        payload.Headers["Cookie"].ShouldBe("[REDACTED]");
    }

    [Test]
    public void Should_ReturnJsonContent()
    {
        var context = new DefaultHttpContext();

        var result = CreateController(context).Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        content.Content.ShouldNotBeNullOrWhiteSpace();
    }

    private static EchoResponse DeserializePayload(IActionResult result)
    {
        var content = result.ShouldBeOfType<ContentResult>();
        var payload = JsonSerializer.Deserialize<EchoResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        return payload!;
    }
}
